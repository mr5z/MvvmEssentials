using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class UnsupportedPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    bool IPageNavigationHandler.CanHandle(Page? page) => true;

    Task<Result<Page?>> IPageNavigationHandler.HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated)
    {
        const string error = "Relative navigation is only supported when root page is a NavigationPage.";
        _logger.LogWarning(error);
        return Task.FromResult(Result.Fail<Page?>(ErrorCode.NotSupported, error));
    }
}