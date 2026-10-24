namespace Nkraft.MvvmEssentials.Services.Navigation;

internal static class NavigationHints
{
    public const string IsTabbedPageSwitch = "__IsTabbedPageSwitch";
    
    public const string IsFlyoutDetailRoot = "__IsFlyoutDetailRoot";

    public const string PopupCompletionParam = "__PopupCompletion";
    
    // The ModalPresentation a page belongs to; set on every page ModalService creates for a modal
    public const string ModalPresentation = "__ModalPresentation";
}