using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class TabbedPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    async Task<Result<NavigationContext>> IPageNavigationHandler.HandleAsync(Page page, NavigationRequest request, bool animated)
    {
        var tabbedPage = (TabbedPage)page;

        var isExplicitTabSwitch = request.Parameters.ContainsKey(NavigationHints.IsTabbedPageSwitch);
        if (isExplicitTabSwitch)
        {
            return HandleTabSwitch(tabbedPage, request.Pages[0].PageType);
        }

        var currentTab = tabbedPage.CurrentPage;
        if (currentTab is null)
        {
            const string message = "No current tab found in the TabbedPage.";
            _logger.LogWarning(message);
            return Result.Fail<NavigationContext>(ErrorCode.NotSupported, message);
        }

        if (currentTab is not NavigationPage tabNavigationPage)
        {
            const string message = "Relative navigation within a TabbedPage is only supported when the current tab is wrapped in a NavigationPage.";
            _logger.LogWarning(message);
            return Result.Fail<NavigationContext>(ErrorCode.NotSupported, message);
        }

        foreach (var pageInfo in request.Pages)
        {
            await tabNavigationPage.PushAsync(request.Materialize(pageInfo), animated);
        }

        return Result.Ok(NavigationContext.Complete());
    }

    private Result<NavigationContext> HandleTabSwitch(TabbedPage tabbedPage, Type targetPageType)
    {
        var targetTab = tabbedPage.Children.FirstOrDefault(tab =>
        {
            var candidatePage = tab is NavigationPage navPage ? navPage.RootPage : tab;
            return targetPageType == candidatePage.GetType();
        });

        if (targetTab is null)
        {
            const string message = "Attempted to switch to tab '{TargetPage}', but it is not registered in the TabbedPage.";
            _logger.LogWarning(message, targetPageType.Name);
            return Result.Fail<NavigationContext>(ErrorCode.NotSupported, message);
        }

        tabbedPage.CurrentPage = targetTab;
        return Result.Ok(NavigationContext.Complete());
    }
}
