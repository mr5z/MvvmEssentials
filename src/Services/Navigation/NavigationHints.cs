namespace Nkraft.MvvmEssentials.Services.Navigation;

internal static class NavigationHints
{
    public const string IsTabbedPageSwitch = "__IsTabbedPageSwitch";
    
    public const string IsFlyoutDetailRoot = "__IsFlyoutDetailRoot";

    // The Presentation a page belongs to; set on every page a presenting service creates for a popup or modal
    public const string Presentation = "__Presentation";
}