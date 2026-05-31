using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Pages;

internal enum NavigationAction
{
    Completed,
    
    ContinueInto
}

internal record NavigationContext(NavigationAction Action, Page? NextPage = null)
{
    public static NavigationContext Complete() => new(NavigationAction.Completed);
    
    public static NavigationContext Into(Page page) => new(NavigationAction.ContinueInto, page);
}

internal interface IPageNavigationHandler
{
    bool CanHandle(Page? page);
    
    Task<Result<NavigationContext>> HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated);
}
