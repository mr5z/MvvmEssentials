using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class TabbedPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    public bool CanHandle(Page? page) => page is TabbedPage;

    public async Task<IResult> HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated)
    {
        var tabbedPage = (TabbedPage)page;
        
        var isExplicitTabSwitch = parameters?.ContainsKey(NavigationHints.IsTabbedPageSwitch) == true;
        if (isExplicitTabSwitch)
        {
            var targetVmType = newPages.First().BindingContext?.GetType();
        
            // Find the matching tab
            var targetTab = tabbedPage.Children.FirstOrDefault(c => 
            {
                var actualPage = c is NavigationPage navPage ? navPage.RootPage : c;
                return actualPage?.BindingContext?.GetType() == targetVmType;
            });

            if (targetTab is not null)
            {
                tabbedPage.CurrentPage = targetTab;
                return Result.Ok();
            }

            const string error = "Attempted to switch to tab '{TargetVm}', but it is not registered in the TabbedPage.";
            _logger.LogWarning(error, targetVmType?.Name);
        }

        var currentTab = tabbedPage.CurrentPage;
        if (currentTab is not NavigationPage tabNavigationPage)
        {
            if (currentTab is null)
            {
                const string error = "No current tab found in the TabbedPage.";
                _logger.LogWarning(error);
                return Result.Fail(ErrorCode.NotSupported, error);
            }
            else
            {
                const string error = "Relative navigation within a TabbedPage is only supported when the current tab is wrapped in a NavigationPage.";
                _logger.LogWarning(error);
                return Result.Fail(ErrorCode.NotSupported, error);
            }
        }

        foreach (var newPage in newPages)
        {
            await tabNavigationPage.PushAsync(newPage, animated);
        }

        return Result.Ok();
    }
}