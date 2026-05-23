using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class TabbedPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    bool IPageNavigationHandler.CanHandle(Page? page) => page is TabbedPage;

    async Task<Result<Page?>> IPageNavigationHandler.HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated)
    {
        var tabbedPage = (TabbedPage)page;
    
        var isExplicitTabSwitch = parameters?.ContainsKey(NavigationHints.IsTabbedPageSwitch) == true;
        if (isExplicitTabSwitch)
        {
            return HandleTabSwitch(tabbedPage, newPages);
        }

        var currentTab = tabbedPage.CurrentPage;
        if (currentTab is null)
        {
            const string error = "No current tab found in the TabbedPage.";
            _logger.LogWarning(error);
            return Result.Fail<Page?>(ErrorCode.NotSupported, error);
        }

        if (currentTab is not NavigationPage tabNavigationPage)
        {
            const string error = "Relative navigation within a TabbedPage is only supported when the current tab is wrapped in a NavigationPage.";
            _logger.LogWarning(error);
            return Result.Fail<Page?>(ErrorCode.NotSupported, error);
        }

        foreach (var newPage in newPages)
        {
            await tabNavigationPage.PushAsync(newPage, animated);
        }

        return Result.Ok<Page?>(null);
    }
    
    private Result<Page?> HandleTabSwitch(TabbedPage tabbedPage, Page[] newPages)
    {
        var targetVmType = newPages.First().BindingContext?.GetType();

        var targetTab = tabbedPage.Children.FirstOrDefault(tab => 
        {
            var targetPage = tab is NavigationPage navPage ? navPage.RootPage : tab;
            return targetPage?.BindingContext?.GetType() == targetVmType;
        });

        if (targetTab is null)
        {
            const string error = "Attempted to switch to tab '{TargetVm}', but it is not registered in the TabbedPage.";
            _logger.LogWarning(error, targetVmType?.Name);
            return Result.Fail<Page?>(ErrorCode.NotSupported, error);
        }

        tabbedPage.CurrentPage = targetTab;
        return Result.Ok<Page?>(null);
    }
}