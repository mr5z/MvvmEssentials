namespace Nkraft.MvvmEssentials.Services.Pages;

public class PageDestination(string pageName, INavigationParameters parameters)
{
    public string PageName { get; } = pageName;
    public INavigationParameters Parameters { get; } = parameters;
}
