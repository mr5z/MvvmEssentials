using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services;

internal interface IPageNavigationHandler
{
    bool CanHandle(Page? page);
    Task<IResult> HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated);
}
