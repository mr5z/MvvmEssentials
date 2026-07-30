using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nkraft.MvvmEssentials.SourceGenerator;

[Generator]
public sealed class AppStartupGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var userStartups = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => AppStartupSymbolCollector.GetIfImplementsIAppStartup(ctx))
            .Where(static x => x is not null)
            .Collect();

        var initialViewModels = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is InvocationExpressionSyntax,
                transform: static (ctx, _) => InitialViewModelDetector.TryGetInitialViewModelFromInvocation(ctx))
            .Where(static x => x is not null)
            .Collect();

        context.RegisterSourceOutput(
            userStartups.Combine(initialViewModels),
            static (ctx, source) => Execute(ctx, source.Left, source.Right));
    }

    private static void Execute(
        SourceProductionContext ctx,
        ImmutableArray<INamedTypeSymbol?> userStartups,
        ImmutableArray<string?> initialViewModels)
    {
        var validStartups = userStartups.Where(s => s is not null).ToList();
        var validInitials = initialViewModels.Where(s => s is not null).ToList();

        // MVE002 — more than one IAppStartup
        if (validStartups.Count > 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MultipleStartupsDefined, Location.None,
                string.Join(", ", validStartups.Select(s => s!.Name))));
            return;
        }

        // MVE001 — nothing defined at all
        if (validStartups.Count == 0 && validInitials.Count == 0)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoStartupDefined, Location.None));
            ctx.AddSource("AppStartup.Registration.g.cs",
                SourceText.From(AppStartupCodeTemplates.NoOpRegistration(), Encoding.UTF8));
            return;
        }

        if (validStartups.Count == 1)
        {
            ctx.AddSource("AppStartup.Registration.g.cs",
                SourceText.From(AppStartupCodeTemplates.UserStartupRegistration(validStartups[0]!), Encoding.UTF8));
            return;
        }

        // No IAppStartup found — generate default from isInitial: true
        if (validInitials.Count > 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MultipleStartupsDefined, Location.None,
                string.Join(", ", validInitials)));
            return;
        }

        var initialVmFullName = validInitials[0]!;
        ctx.AddSource("AppStartup.Default.g.cs",
            SourceText.From(AppStartupCodeTemplates.DefaultStartup(initialVmFullName), Encoding.UTF8));
        ctx.AddSource("AppStartup.Registration.g.cs",
            SourceText.From(AppStartupCodeTemplates.DefaultStartupRegistration(), Encoding.UTF8));
    }
}