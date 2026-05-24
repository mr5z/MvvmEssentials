using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Navigation;

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
