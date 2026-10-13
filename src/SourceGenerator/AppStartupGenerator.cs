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
    private const string RegistrationMethodName = "AddDiscoveredAppStartup";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Only the project that calls AddDiscoveredAppStartup() (the composition root) gets the
        // generated registration. Libraries such as a separate ViewModel project get nothing,
        // so they no longer emit a stray copy or a spurious MVE001.
        var callsRegistration = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRegistrationCall(node),
                transform: static (_, _) => true)
            .Collect()
            .Select(static (calls, _) => calls.Length > 0);

        var sourceStartups = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => AppStartupSymbolCollector.GetIfImplementsIAppStartup(ctx))
            .Where(static x => x is not null)
            .Collect();

        // IAppStartup implementations in referenced projects (e.g. a ViewModel library).
        var referencedStartups = context.CompilationProvider
            .Select(static (compilation, ct) => ReferencedAppStartupCollector.Collect(compilation, ct));

        var initialViewModels = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is InvocationExpressionSyntax,
                transform: static (ctx, _) => InitialViewModelDetector.TryGetInitialViewModelFromInvocation(ctx))
            .Where(static x => x is not null)
            .Collect();

        var inputs = callsRegistration
            .Combine(sourceStartups)
            .Combine(referencedStartups)
            .Combine(initialViewModels);

        context.RegisterSourceOutput(inputs, static (ctx, source) =>
        {
            var (((isCompositionRoot, fromSource), fromReferences), initials) = source;
            if (isCompositionRoot)
            {
                Execute(ctx, fromSource, fromReferences, initials);
            }
        });
    }

    private static bool IsRegistrationCall(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null,
        };
        return name == RegistrationMethodName;
    }

    private static void Execute(
        SourceProductionContext ctx,
        ImmutableArray<string?> sourceStartups,
        ReferencedAppStartups referencedStartups,
        ImmutableArray<string?> initialViewModels)
    {
        // MVE007 — an IAppStartup in a referenced project that this project cannot see
        foreach (var inaccessible in referencedStartups.Inaccessible.Array)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.InaccessibleStartup, Location.None, inaccessible));
        }

        var validStartups = sourceStartups
            .Where(s => s is not null)
            .Select(s => s!)
            .Concat(referencedStartups.Accessible.Array)
            .Distinct()
            .ToList();
        var validInitials = initialViewModels.Where(s => s is not null).ToList();

        // MVE002 — more than one IAppStartup, across this project and its references
        if (validStartups.Count > 1)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MultipleStartupsDefined, Location.None,
                string.Join(", ", validStartups)));
            return;
        }

        if (validStartups.Count == 1)
        {
            ctx.AddSource("AppStartup.Registration.g.cs",
                SourceText.From(AppStartupCodeTemplates.UserStartupRegistration(validStartups[0]), Encoding.UTF8));
            return;
        }

        // MVE001 — nothing defined at all
        if (validInitials.Count == 0)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoStartupDefined, Location.None));
            ctx.AddSource("AppStartup.Registration.g.cs",
                SourceText.From(AppStartupCodeTemplates.NoOpRegistration(), Encoding.UTF8));
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