using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? From(Location location)
    {
        return location.SourceTree is null
            ? null
            : new LocationInfo(
                location.SourceTree.FilePath,
                location.SourceSpan,
                location.GetLineSpan().Span);
    }

    public static LocationInfo? From(SyntaxToken token) => From(token.GetLocation());
}