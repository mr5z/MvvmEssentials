using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class NavigationPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    bool IPageNavigationHandler.CanHandle(Page? page) => page is NavigationPage;

    async Task<Result<NavigationContext>> IPageNavigationHandler.HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated)
    {
        var navigationPage = (NavigationPage)page;
        
        if (newPages.Length == 0)
        {
            const string error = "No pages to navigate.";
            _logger.LogWarning(error);
            return Result.Fail<NavigationContext>(ErrorCode.General, error);
        }
        
        foreach (var newPage in newPages)
        {
            await navigationPage.PushAsync(newPage, animated);
        }
        
        return Result.Ok(NavigationContext.Complete());
    }
}