using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class NavigationPageHandler : IPageNavigationHandler
{
    public bool CanHandle(Page? page) => page is NavigationPage;

    public async Task<IResult> HandleAsync(Page page, Page[] newPages, bool animated)
    {
        var navigationPage = (NavigationPage)page;
        
        foreach (var newPage in newPages)
        {
            await navigationPage.PushAsync(newPage, animated);
        }
        
        return Result.Ok();
    }
}