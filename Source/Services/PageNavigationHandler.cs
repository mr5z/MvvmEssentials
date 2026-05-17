using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services;

internal interface IPageNavigationHandler
{
    bool CanHandle(Page? page);
    Task<IResult> HandleAsync(Page page, Page[] newPages, bool animated);
}