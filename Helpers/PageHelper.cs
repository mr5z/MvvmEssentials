namespace Nkraft.MvvmEssentials.Helpers;

internal static class PageHelper
{
    internal static string ToPageName<TViewModel>(string pagePattern)
    {
        return ToPageName(typeof(TViewModel), pagePattern);
    }
    
    internal static string ToPageName(Type viewModelType, string pagePattern)
    {
        const string knownViewModelPattern = "ViewModel";
        return viewModelType.Name.Replace(knownViewModelPattern, pagePattern);
    }
}