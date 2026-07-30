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
    
    /// <summary>
    /// MVE003: ViewModel with navigation parameters without partial modifier found.
    /// </summary>
    public static readonly DiagnosticDescriptor NotPartial = new(
        id: "MVE003",
        title: "ViewModel with navigation parameters must be partial",
        messageFormat: "'{0}' declares [NavigationParameter] properties but is not partial, so the "
                       + "With(...) factory cannot be generated. Add the 'partial' modifier.",
        category: "Nkraft.MvvmEssentials",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor InvalidPreferredName = new(
        id: "MVE004",
        title: "Invalid PreferredName on [NavigationParameter]",
        messageFormat: "'{0}' on '{1}' is not a valid C# identifier and cannot be used as a With(...) parameter name",
        category: "Nkraft.MvvmEssentials",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    public static readonly DiagnosticDescriptor DuplicateParameterName = new(
        id: "MVE005",
        title: "Duplicate With(...) parameter name",
        messageFormat: "Parameter name '{0}' on '{1}' is used by more than one [NavigationParameter] property (check PreferredName)",
        category: "Nkraft.MvvmEssentials",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    // public static readonly DiagnosticDescriptor NavigationParameterOnNonPageViewModel = new(
    //     id: "MVE006",
    //     title: "[NavigationParameter] requires PageViewModel",
    //     messageFormat: "'{0}' has [NavigationParameter] properties but does not derive from PageViewModel. "
    //                    + "No With(...) factory will be generated. Derive from PageViewModel and register with "
    //                    + "MapPage (not RegisterPage) to get With(...), or remove [NavigationParameter] if you "
    //                    + "only need OnParametersSet.",
    //     category: "Nkraft.MvvmEssentials",
    //     defaultSeverity: DiagnosticSeverity.Warning,
    //     isEnabledByDefault: true);
}