using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nkraft.MvvmEssentials.SourceGenerator.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NavigationParameterPlacementAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        Diagnostics.NavigationParameterOnNonPageViewModel
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;

        var hasAttributedProperty = symbol.GetMembers().OfType<IPropertySymbol>()
            .Any(p => p.GetAttributes().Any(a =>
                a.AttributeClass?.ToDisplayString() == WellKnownNames.NavigationParameterAttribute));

        if (hasAttributedProperty == false)
            return;

        if (symbol.IsDerivedFrom(WellKnownNames.PageViewModel))
            return;

        if (symbol.IsDerivedFrom(WellKnownNames.NavigableEntryViewModel) == false)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.NavigationParameterOnNonPageViewModel,
            symbol.Locations.FirstOrDefault() ?? Location.None,
            symbol.Name));
    }
}