using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal sealed record ViewModelParameter(
    string PropertyName,
    string Type,
    bool IsOptional,
    string? PreferredName);

internal sealed record ViewModelTarget(
    string Namespace,
    string ClassName,
    string TypeKeyword,
    string Suffix,
    string? ResultType,
    string Accessibility,
    bool IsPartial,
    LocationInfo? Location,
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
            .SelectMany(static (all, _) => all.Distinct())
            ;

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
        
        var declarations = symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .ToArray();

        var isPartial = declarations.Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword));
        var location = declarations.Length > 0 ? LocationInfo.From(declarations[0]) : null;

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
            IsPartial: isPartial,
            Location: location,
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

                var isOptional = attr.NamedArguments
                    .FirstOrDefault(na => na.Key == "IsOptional").Value.Value as bool? ?? false;
                
                var preferredName = attr.NamedArguments
                    .FirstOrDefault(na => na.Key == "PreferredName").Value.Value as string;

                result.Add(new ViewModelParameter(
                    PropertyName: p.Name,
                    Type: p.Type.ToDisplayString(TypeFormat),
                    IsOptional: isOptional,
                    PreferredName: preferredName));
            }
        }

        // required first (C# demands optionals last)
        return result.OrderBy(p => p.IsOptional).ToArray();
    }

    private static void Emit(SourceProductionContext spc, ViewModelTarget t)
    {
        if (t.IsPartial == false)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.NotPartial,
                t.Location?.ToLocation() ?? Location.None,
                t.ClassName));
            return;
        }
        
        var invalidNames = t.Parameters.Array
            .Where(p => p.PreferredName is not null && IsValidIdentifier(p.PreferredName) == false)
            .ToArray();

        if (invalidNames.Length > 0)
        {
            foreach (var p in invalidNames)
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidPreferredName, 
                    t.Location?.ToLocation() ?? Location.None, 
                    p.PreferredName, 
                    t.ClassName));
            return;
        }
        
        var duplicateNames = t.Parameters.Array
            .GroupBy(GetParameterName, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            foreach (var name in duplicateNames)
                spc.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.DuplicateParameterName,
                    t.Location?.ToLocation() ?? Location.None, 
                    name, 
                    t.ClassName));
            return;
        }
        
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
        sb.Append(string.Join(", ", t.Parameters.Array.Select(p => p.IsOptional
            ? $"{ToNullable(p.Type)} {GetParameterName(p)} = default"
            : $"{p.Type} {GetParameterName(p)}"))
        );
        sb.AppendLine(")");
        sb.AppendLine("    {");
        sb.AppendLine("        var __p = new global::Nkraft.MvvmEssentials.Services.NavigationParameters();");

        foreach (var p in t.Parameters.Array)
        {
            var name = GetParameterName(p);
            var guard = p.IsOptional ? $"if ({name} is not null) " : string.Empty;
            sb.AppendLine($"        {guard}__p.Add(\"{p.PropertyName}\", {name});");
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

    private static string GetParameterName(ViewModelParameter parameter)
    {
        var propertyName = string.IsNullOrEmpty(parameter.PreferredName) ? parameter.PropertyName : parameter.PreferredName!;
        var name = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;
    }
    
    private static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (SyntaxFacts.IsIdentifierStartCharacter(name[0]) == false)
            return false;

        for (var i = 1; i < name.Length; i++)
            if (SyntaxFacts.IsIdentifierPartCharacter(name[i]) == false)
                return false;

        return true;
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

internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? From(SyntaxNode node)
    {
        var location = node.GetLocation();
        return location.SourceTree is null
            ? null
            : new LocationInfo(
                location.SourceTree.FilePath,
                location.SourceSpan,
                location.GetLineSpan().Span);
    }
}
