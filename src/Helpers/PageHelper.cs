namespace Nkraft.MvvmEssentials.Helpers;

internal class PagePattern
{
    public string Pattern { get; }
    private PagePattern(string pattern) => Pattern = pattern;
    public static readonly PagePattern Page = new("Page");
    public static readonly PagePattern Popup = new("Popup");
    public static readonly PagePattern ViewModel = new("ViewModel");
}

internal static class PageHelper
{
    internal static string ToPageName(Type viewModelType, PagePattern pagePattern)
        => viewModelType.Name.Replace(PagePattern.ViewModel.Pattern, pagePattern.Pattern);
    
    internal static string ToPageName<TViewModel>(PagePattern pagePattern)
        => ToPageName(typeof(TViewModel), pagePattern);
    
    internal static string ToViewModelName(Type pageType)
        => pageType.Name.Replace(PagePattern.Page.Pattern, PagePattern.ViewModel.Pattern);
}