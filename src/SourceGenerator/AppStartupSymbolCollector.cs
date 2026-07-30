using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal static class AppStartupSymbolCollector
{
    public static INamedTypeSymbol? GetIfImplementsIAppStartup(GeneratorSyntaxContext ctx)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol)
            return null;

        return symbol.AllInterfaces.Any(i => i.ToDisplayString() == WellKnownNames.IAppStartup)
            ? symbol
            : null;
    }
}