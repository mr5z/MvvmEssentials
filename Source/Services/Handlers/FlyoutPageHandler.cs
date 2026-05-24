using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class FlyoutPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;
    
    // Track original Detail pages per FlyoutPage because MAUI made it so complicated.
    private static readonly ConditionalWeakTable<FlyoutPage, Page> InitialFlyoutDetails = [];

    bool IPageNavigationHandler.CanHandle(Page? page) => page is FlyoutPage;

    async Task<Result<NavigationContext>> IPageNavigationHandler.HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated)
    {
        var flyoutPage = (FlyoutPage)page;
        var detail = flyoutPage.Detail;
        
        if (detail is null)
        {
            const string error = "FlyoutPage has no Detail page set.";
            _logger.LogWarning(error);
            return Result.Fail<NavigationContext>(ErrorCode.InvalidState, error);
        }

        var isFlyoutDetailRootRequest = parameters?.ContainsKey(NavigationHints.IsFlyoutDetailRoot) == true;
        if (isFlyoutDetailRootRequest)
        {
            return await HandleFlyoutMenuNavigation(flyoutPage, detail, newPages);
        }

        return Result.Ok(NavigationContext.Into(detail));
    }

    private async Task<Result<NavigationContext>> HandleFlyoutMenuNavigation(FlyoutPage flyoutPage, Page detail, Page[] newPages)
    {
        // Store original Detail on first navigation
        _ = InitialFlyoutDetails.TryAdd(flyoutPage, detail);
        
        if (InitialFlyoutDetails.TryGetValue(flyoutPage, out var initialDetailPage) == false)
        {
            const string error = "The initial detail page was not found. The navigation cannot proceed.";
            _logger.LogWarning(error);
            return Result.Fail<NavigationContext>(ErrorCode.InvalidState, error);
        }

        // Check if navigating back to original Detail
        var targetPage = newPages.First();
        var requestedViewModelName = PageHelper.ToViewModelName(targetPage.GetType());
        var originalViewModelName = initialDetailPage.BindingContext?.GetType().Name;
        var isNavigatingToOriginal = requestedViewModelName == originalViewModelName;

        if (isNavigatingToOriginal)
        {
            flyoutPage.Detail = initialDetailPage;
            return Result.Ok(NavigationContext.Complete());
        }
        
        // Workaround for MAUI bug: https://github.com/dotnet/maui/issues/22116
        if (flyoutPage.Detail is NavigationPage navPage)
        {
            await navPage.PopToRootAsync(animated: false);
            navPage.Navigation.InsertPageBefore(targetPage, navPage.RootPage);
            await navPage.PopAsync(animated: false);
        }
        else
        {
            // Detail is not a NavigationPage - wrap in one (only happens once)
            navPage = new NavigationPage(targetPage);
            flyoutPage.Detail = navPage;
        }
        
        // Push remaining pages if any
        foreach (var nextPage in newPages.Skip(1))
        {
            await navPage.PushAsync(nextPage, animated: false);
        }

        return Result.Ok(NavigationContext.Complete());
    }
}
