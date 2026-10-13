using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal sealed record ReferencedAppStartups(
    EquatableArray<string> Accessible,
    EquatableArray<string> Inaccessible);

/// <summary>
/// Finds IAppStartup implementations in referenced assemblies, so a startup class may live in a
/// separate project (e.g. a ViewModel library) from the one that calls AddDiscoveredAppStartup().
/// </summary>
internal static class ReferencedAppStartupCollector
{
    public static ReferencedAppStartups Collect(Compilation compilation, CancellationToken ct)
    {
        var accessible = new List<string>();
        var inaccessible = new List<string>();

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            ct.ThrowIfCancellationRequested();

            // Only assemblies built against this library can implement IAppStartup. This skips
            // the framework and third-party references, which is where nearly all types are.
            if (ReferencesLibrary(assembly) == false)
                continue;

            foreach (var type in GetTypes(assembly.GlobalNamespace))
            {
                if (IsAppStartup(type) == false)
                    continue;

                var name = type.ToDisplayString();
                if (compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
                    accessible.Add(name);
                else
                    inaccessible.Add(name);
            }
        }

        return new ReferencedAppStartups(
            new EquatableArray<string>(accessible.ToArray()),
            new EquatableArray<string>(inaccessible.ToArray()));
    }

    private static bool ReferencesLibrary(IAssemblySymbol assembly)
    {
        return assembly.Modules.Any(m => m.ReferencedAssemblies.Any(
            a => a.Name == WellKnownNames.LibraryAssemblyName));
    }

    private static bool IsAppStartup(INamedTypeSymbol type)
    {
        if (type.TypeKind != TypeKind.Class || type.IsAbstract)
            return false;

        if (type.AllInterfaces.Any(i => i.ToDisplayString() == WellKnownNames.IAppStartup) == false)
            return false;

        // A startup this generator produced in another project is that project's own default,
        // not a user-defined startup.
        return type.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute"
            && a.ConstructorArguments.FirstOrDefault().Value as string == WellKnownNames.GeneratorName) == false;
    }

    private static IEnumerable<INamedTypeSymbol> GetTypes(INamespaceSymbol ns)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            foreach (var nested in GetTypesAndNested(type))
                yield return nested;
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            foreach (var type in GetTypes(child))
                yield return type;
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetTypesAndNested(INamedTypeSymbol type)
    {
        yield return type;
        foreach (var nested in type.GetTypeMembers())
        {
            foreach (var inner in GetTypesAndNested(nested))
                yield return inner;
        }
    }
}