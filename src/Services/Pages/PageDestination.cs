namespace Nkraft.MvvmEssentials.Services.Pages;

public sealed class PageDestination(string pageName, INavigationParameters parameters)
{
    public string PageName { get; } = pageName;
    public INavigationParameters Parameters { get; } = parameters;
}

public sealed class PopupDestination<TResult>(string popupName, INavigationParameters parameters)
{
    public string PopupName { get; } = popupName;
    public INavigationParameters Parameters { get; } = parameters;
}
