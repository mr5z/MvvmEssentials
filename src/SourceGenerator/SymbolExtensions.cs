using Microsoft.CodeAnalysis;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal static class SymbolExtensions
{
    public static bool IsDerivedFrom(this INamedTypeSymbol symbol, string baseTypeName)
    {
        for (var t = symbol.BaseType; t is not null; t = t.BaseType)
            if (t.ToDisplayString() == baseTypeName)
                return true;
        return false;
    }
}
