namespace Nkraft.MvvmEssentials.Helpers;

internal static class PageHelper
{
    internal static string ToPageName<TViewModel>(string pagePattern)
    {
        const string knownViewModelPattern = "ViewModel";
        return typeof(TViewModel).Name.Replace(knownViewModelPattern, pagePattern);
    }
}