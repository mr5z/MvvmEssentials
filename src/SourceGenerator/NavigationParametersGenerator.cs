using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal sealed record ViewModelParameter(
    string PropertyName,
    string Type,
    bool IsRequired);

internal sealed record ViewModelTarget(
    string Namespace,
    string ClassName,
    string TypeKeyword,
    string Suffix,
    string? ResultType,
    string Accessibility,
    EquatableArray<ViewModelParameter> Parameters);

[Generator]
public sealed class NavigationParametersGenerator : IIncrementalGenerator
{
    private const string TargetBaseViewModelName = "Nkraft.MvvmEssentials.ViewModels.PageViewModel";
    private const string NavigationParameterName = "Nkraft.MvvmEssentials.Attributes.NavigationParameterAttribute";
    private const string PopupViewModelNamespace = "Nkraft.MvvmEssentials.ViewModels";
    private const string PopupViewModelMetadataName = "IPopupViewModel`1";
    private const string GeneratorName = "Nkraft.MvvmEssentials.SourceGenerator";

    private static readonly SymbolDisplayFormat TypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
                NavigationParameterName,
                predicate: static (node, _) => node.IsKind(SyntaxKind.PropertyDeclaration),
                transform: static (ctx, _) => ToViewModelTarget(ctx.TargetSymbol.ContainingType))
            .Where(static t => t is not null)
            .Select(static (t, _) => t!)
            .Collect()
            // one attributed property -> one pipeline item, so the same VM appears N times.
            // dedup, then fan back out so each VM caches independently.
            .SelectMany(static (all, _) => all.Distinct());

        context.RegisterSourceOutput(targets, static (spc, t) => Emit(spc, t));
    }

    private static ViewModelTarget? ToViewModelTarget(INamedTypeSymbol? symbol)
    {
        if (symbol is null)
            return null;

        if (symbol.IsAbstract || symbol.IsStatic || symbol.IsGenericType || symbol.ContainingType is not null)
            return null;

        if (IsDerivedFrom(symbol, TargetBaseViewModelName) == false)
            return null;

        var props = CollectParameters(symbol);
        if (props.Length == 0)
            return null;

        var popupInterface = symbol.AllInterfaces.FirstOrDefault(i =>
            i.MetadataName == PopupViewModelMetadataName &&
            i.ContainingNamespace.ToDisplayString() == PopupViewModelNamespace);

        var ns = symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        return new ViewModelTarget(
            Namespace: ns,
            ClassName: symbol.Name,
            TypeKeyword: symbol.IsRecord ? "record" : "class",
            Suffix: popupInterface is not null ? "Popup" : "Page",
            ResultType: popupInterface?.TypeArguments[0].ToDisplayString(TypeFormat),
            Accessibility: ToKeyword(symbol.DeclaredAccessibility),
            Parameters: new EquatableArray<ViewModelParameter>(props));
    }

    private static ViewModelParameter[] CollectParameters(INamedTypeSymbol symbol)
    {
        var result = new List<ViewModelParameter>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        // walk the hierarchy so parameters declared on an intermediate VM are included;
        // most-derived wins on name collision
        for (var t = symbol; t is not null; t = t.BaseType)
        {
            if (t.ToDisplayString() == TargetBaseViewModelName)
                break;

            foreach (var p in t.GetMembers().OfType<IPropertySymbol>())
            {
                var attr = p.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NavigationParameterName);
                if (attr is null || names.Add(p.Name) == false)
                    continue;

                var isRequired = attr.NamedArguments
                    .FirstOrDefault(na => na.Key == "IsRequired").Value.Value as bool? ?? true;

                result.Add(new ViewModelParameter(
                    PropertyName: p.Name,
                    Type: p.Type.ToDisplayString(TypeFormat),
                    IsRequired: isRequired));
            }
        }

        // required first (C# demands optionals last); OrderByDescending is stable,
        // so declaration order holds within each group
        return result.OrderByDescending(p => p.IsRequired).ToArray();
    }

    private static void Emit(SourceProductionContext spc, ViewModelTarget t)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        if (t.Namespace.Length > 0)
            sb.Append("namespace ").Append(t.Namespace).AppendLine(";").AppendLine();

        sb.Append(t.Accessibility).Append(" partial ").Append(t.TypeKeyword).Append(' ').AppendLine(t.ClassName);
        sb.AppendLine("{");

        var returnType = t.ResultType is null
            ? "global::Nkraft.MvvmEssentials.Services.Pages.PageDestination"
            : $"global::Nkraft.MvvmEssentials.Services.Pages.PopupDestination<{t.ResultType}>";

        sb.Append("    [global::System.CodeDom.Compiler.GeneratedCode(\"").Append(GeneratorName).AppendLine("\", \"1.0\")]");
        sb.Append("    public static ").Append(returnType).Append(" With(");
        sb.Append(string.Join(", ", t.Parameters.Array.Select(p => p.IsRequired
            ? $"{p.Type} {ToParameterName(p.PropertyName)}"
            : $"{ToNullable(p.Type)} {ToParameterName(p.PropertyName)} = default")));
        sb.AppendLine(")");
        sb.AppendLine("    {");
        sb.AppendLine("        var __p = new global::Nkraft.MvvmEssentials.Services.NavigationParameters();");

        foreach (var p in t.Parameters.Array)
        {
            var name = ToParameterName(p.PropertyName);
            if (p.IsRequired)
                sb.Append("        __p.Add(\"").Append(p.PropertyName).Append("\", ").Append(name).AppendLine(");");
            else
                sb.Append("        if (").Append(name).Append(" is not null) __p.Add(\"")
                    .Append(p.PropertyName).Append("\", ").Append(name).AppendLine(");");
        }

        var destinationName = t.ClassName.Replace("ViewModel", t.Suffix);

        sb.AppendLine($"        return new {returnType}(\"{destinationName}\", __p);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var hint = (t.Namespace.Length > 0 ? t.Namespace + "." : string.Empty) + t.ClassName + ".NavigationParameters.g.cs";
        spc.AddSource(hint, sb.ToString());
    }

    private static bool IsDerivedFrom(INamedTypeSymbol symbol, string baseTypeName)
    {
        for (var t = symbol.BaseType; t is not null; t = t.BaseType)
            if (t.ToDisplayString() == baseTypeName)
                return true;
        return false;
    }

    private static string ToNullable(string type) =>
        type.EndsWith("?", StringComparison.Ordinal) ? type : type + "?";

    private static string ToKeyword(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        _ => "internal"
    };

    private static string ToParameterName(string propertyName)
    {
        var name = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }
}

// arrays lack structural equality, which silently defeats incremental caching.
internal readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly T[]? _array = array;

    public T[] Array => _array ?? [];

    public bool Equals(EquatableArray<T> other)
    {
        var mine = Array;
        var theirs = other.Array;
        if (mine.Length != theirs.Length)
            return false;
        for (var i = 0; i < mine.Length; i++)
            if (mine[i].Equals(theirs[i]) == false)
                return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> o && Equals(o);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in Array)
                hash = hash * 31 + item.GetHashCode();
            return hash;
        }
    }
}
