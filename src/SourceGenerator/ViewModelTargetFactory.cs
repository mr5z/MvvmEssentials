using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal static class ViewModelTargetFactory
{
    private static readonly SymbolDisplayFormat TypeFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static ViewModelTarget? Create(INamedTypeSymbol? symbol)
    {
        if (symbol is null)
            return null;

        if (symbol.IsAbstract || symbol.IsStatic || symbol.IsGenericType || symbol.ContainingType is not null)
            return null;

        if (symbol.IsDerivedFrom(WellKnownNames.PageViewModel) == false)
            return null;

        var props = CollectParameters(symbol);
        if (props.Length == 0)
            return null;

        var declarations = symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .ToArray();

        var isPartial = declarations.Any(d => d.Modifiers.Any(SyntaxKind.PartialKeyword));
        var location = declarations.Length > 0
            ? LocationInfo.From(declarations[0].Identifier)
            : null;

        var popupInterface = FindResultInterface(symbol, WellKnownNames.PopupViewModelMetadataName);
        var modalInterface = popupInterface is null
            ? FindResultInterface(symbol, WellKnownNames.ModalViewModelMetadataName)
            : null;

        var (suffix, destinationType) = (popupInterface, modalInterface) switch
        {
            ({ } popup, _) => ("Popup", $"{WellKnownNames.DestinationNamespace}.PopupDestination<{popup.TypeArguments[0].ToDisplayString(TypeFormat)}>"),
            // modals are regular pages presented modally, so they keep the "Page" suffix (PagePattern.Page)
            (_, { } modal) => ("Page", $"{WellKnownNames.DestinationNamespace}.ModalDestination<{modal.TypeArguments[0].ToDisplayString(TypeFormat)}>"),
            _ => ("Page", $"{WellKnownNames.DestinationNamespace}.PageDestination"),
        };

        var ns = symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        return new ViewModelTarget(
            Namespace: ns,
            ClassName: symbol.Name,
            TypeKeyword: symbol.IsRecord ? "record" : "class",
            Suffix: suffix,
            DestinationType: destinationType,
            Accessibility: ToKeyword(symbol.DeclaredAccessibility),
            IsPartial: isPartial,
            Location: location,
            Parameters: new EquatableArray<ViewModelParameter>(props));
    }

    private static INamedTypeSymbol? FindResultInterface(INamedTypeSymbol symbol, string metadataName) =>
        symbol.AllInterfaces.FirstOrDefault(i =>
            i.MetadataName == metadataName &&
            i.ContainingNamespace.ToDisplayString() == WellKnownNames.PopupViewModelNamespace);

    private static ViewModelParameter[] CollectParameters(INamedTypeSymbol symbol)
    {
        var result = new List<ViewModelParameter>();
        var names = new HashSet<string>(StringComparer.Ordinal);

        // walk the hierarchy so parameters declared on an intermediate VM are included;
        // most-derived wins on name collision
        for (var t = symbol; t is not null; t = t.BaseType)
        {
            if (t.ToDisplayString() == WellKnownNames.PageViewModel)
                break;

            foreach (var p in t.GetMembers().OfType<IPropertySymbol>())
            {
                var attr = p.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == WellKnownNames.NavigationParameterAttribute);
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

    private static string ToKeyword(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Public => "public",
        _ => "internal"
    };
}