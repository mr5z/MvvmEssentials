using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nkraft.MvvmEssentials.SourceGenerator;

/// <summary>
/// Detects invocations of the form:
///   registry.MapPage&lt;TPage, TViewModel&gt;(isInitial: true)
///   registry.MapPage&lt;TPage, TViewModel&gt;(true)
/// and extracts TViewModel's full type name so AppStartupGenerator can use it.
/// Only the literal <c>true</c> is recognized: a variable or expression can't be evaluated at compile time.
/// </summary>
internal static class InitialViewModelDetector
{
    private const string MapPageMethodName = "MapPage";
    private const string IsInitialParamName = "isInitial";

    public static string? TryGetInitialViewModelFromInvocation(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not InvocationExpressionSyntax invocation)
            return null;

        // Must be a generic method call
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return null;

        if (memberAccess.Name is not GenericNameSyntax genericName)
            return null;

        if (genericName.Identifier.Text != MapPageMethodName)
            return null;

        // Must have exactly 2 type arguments: <TPage, TViewModel>
        var typeArgs = genericName.TypeArgumentList.Arguments;
        if (typeArgs.Count != 2)
            return null;

        // isInitial is MapPage's only parameter, so it is either named or the first positional argument
        var args = invocation.ArgumentList.Arguments;
        var isInitialArg = args.FirstOrDefault(a => a.NameColon?.Name.Identifier.Text == IsInitialParamName)
            ?? (args.Count > 0 && args[0].NameColon is null ? args[0] : null);

        // Must be the literal `true`
        if (isInitialArg?.Expression is not LiteralExpressionSyntax literal)
            return null;

        if (literal.Token.ValueText != "true")
            return null;

        // Resolve TViewModel (second type arg)
        var viewModelTypeSyntax = typeArgs[1];
        var typeInfo = ctx.SemanticModel.GetTypeInfo(viewModelTypeSyntax);
        return typeInfo.Type?.ToDisplayString();
    }
}