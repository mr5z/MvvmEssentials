namespace Nkraft.MvvmEssentials.Helpers;

internal static class PageHelper
{
    internal static string ToPageName<TViewModel>(string pagePattern)
    {
        const string knownViewModelPattern = "ViewModel";
        return typeof(TViewModel).Name.Replace(knownViewModelPattern, pagePattern);
    }
    
    internal static string ToViewModelName(Type pageType)
    {
        const string knownPagePattern = "Page";
        return pageType.Name.Replace(knownPagePattern, "ViewModel");
    }
}