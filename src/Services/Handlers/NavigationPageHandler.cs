using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Pages;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class NavigationPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    bool IPageNavigationHandler.CanHandle(Page? page) => page is NavigationPage;

    async Task<Result<NavigationContext>> IPageNavigationHandler.HandleAsync(Page page, NavigationRequest request, bool animated)
    {
        var navigationPage = (NavigationPage)page;

        if (request.Pages.Count == 0)
        {
            const string message = "No pages to navigate.";
            _logger.LogWarning(message);
            return Result.Fail<NavigationContext>(ErrorCode.General, message);
        }

        foreach (var pageInfo in request.Pages)
        {
            await navigationPage.PushAsync(request.Materialize(pageInfo), animated);
        }

        return Result.Ok(NavigationContext.Complete());
    }
}
