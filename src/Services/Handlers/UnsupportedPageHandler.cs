using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Pages;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class UnsupportedPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    bool IPageNavigationHandler.CanHandle(Page? page) => true;

    Task<Result<NavigationContext>> IPageNavigationHandler.HandleAsync(Page page, NavigationRequest request, bool animated)
    {
        const string message = "Relative navigation is only supported when root page is a NavigationPage.";
        _logger.LogWarning(message);
        return Task.FromResult(Result.Fail<NavigationContext>(ErrorCode.NotSupported, message));
    }
}
