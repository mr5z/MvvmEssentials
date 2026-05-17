using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class UnsupportedPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    public bool CanHandle(Page? page) => true;

    public Task<IResult> HandleAsync(Page page, Page[] newPages, bool animated)
    {
        const string error = "Relative navigation is only supported when root page is a NavigationPage.";
        _logger.LogWarning(error);
        return Task.FromResult(Result.Fail(ErrorCode.NotSupported, error));
    }
}