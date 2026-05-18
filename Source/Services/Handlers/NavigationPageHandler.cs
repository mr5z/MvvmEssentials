using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class NavigationPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    public bool CanHandle(Page? page) => page is NavigationPage;

    public async Task<IResult> HandleAsync(Page page, Page[] newPages, bool animated)
    {
        var navigationPage = (NavigationPage)page;
        
        if (newPages.Length == 0)
        {
            const string error = "No pages to navigate.";
            _logger.LogWarning(error);
            return Result.Fail(ErrorCode.General, error);
        }
        
        foreach (var newPage in newPages)
        {
            await navigationPage.PushAsync(newPage, animated);
        }
        
        return Result.Ok();
    }
}