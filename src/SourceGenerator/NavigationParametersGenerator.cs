using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nkraft.MvvmEssentials.SourceGenerator;

[Generator]
public sealed class NavigationParametersGenerator : IIncrementalGenerator
{
    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
                WellKnownNames.NavigationParameterAttribute,
                predicate: static (node, _) => node.IsKind(SyntaxKind.PropertyDeclaration),
                transform: static (ctx, _) => ViewModelTargetFactory.Create(ctx.TargetSymbol.ContainingType))
            .Where(static t => t is not null)
            .Select(static (t, _) => t!)
            .Collect()
            // one attributed property -> one pipeline item, so the same VM appears N times.
            // dedup, then fan back out so each VM caches independently.
            .SelectMany(static (all, _) => all.Distinct());

        context.RegisterSourceOutput(targets, static (spc, t) => NavigationParametersEmitter.Emit(spc, t));
    }
}