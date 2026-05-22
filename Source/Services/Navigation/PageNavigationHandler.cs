using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IPageNavigationHandler
{
    bool CanHandle(Page? page);
    Task<IResult> HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated);
}
