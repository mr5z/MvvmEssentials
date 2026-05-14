using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nkraft.MvvmEssentials.SourceGenerator;

/// <summary>
/// Detects invocations of the form:
///   registry.MapPage&lt;TPage, TViewModel&gt;(isInitial: true)
/// and extracts TViewModel's full type name so AppStartupGenerator can use it.
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

        // Check for isInitial: true in the argument list
        var args = invocation.ArgumentList.Arguments;
        var isInitialArg = args.FirstOrDefault(a =>
            a.NameColon?.Name.Identifier.Text == IsInitialParamName);

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