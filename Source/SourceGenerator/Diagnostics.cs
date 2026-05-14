using Microsoft.CodeAnalysis;

namespace Nkraft.MvvmEssentials.SourceGenerator;

internal static class Diagnostics
{
    /// <summary>
    /// MVE001: No IAppStartup found and no isInitial page registered.
    /// </summary>
    public static readonly DiagnosticDescriptor NoStartupDefined = new(
        id: "MVE001",
        title: "No app startup defined",
        messageFormat: "No IAppStartup implementation found and no page is marked with isInitial: true. Either implement IAppStartup or mark a page with isInitial: true in AddPageRegistry.",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// MVE002: Multiple IAppStartup implementations found.
    /// </summary>
    public static readonly DiagnosticDescriptor MultipleStartupsDefined = new(
        id: "MVE002",
        title: "Multiple IAppStartup implementations",
        messageFormat: "Only one class may implement IAppStartup. Found: {0}.",
        category: "Design",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}