using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class FlyoutPageHandler(
    ILogger logger,
    Func<Page?, Page[], INavigationParameters?, bool, Task<IResult>> recursiveNavigationHandler) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;
    private readonly Func<Page?, Page[], INavigationParameters?, bool, Task<IResult>> _recursiveNavigationHandler = recursiveNavigationHandler;
    
    // Track original Detail pages per FlyoutPage because MAUI made it so complicated.
    private static readonly Dictionary<FlyoutPage, Page> OriginalDetails = [];

    public bool CanHandle(Page? page) => page is FlyoutPage;

    public async Task<IResult> HandleAsync(Page page, Page[] newPages, INavigationParameters? parameters, bool animated)
    {
        var flyoutPage = (FlyoutPage)page;
        var detail = flyoutPage.Detail;
        
        if (detail is null)
        {
            const string error = "FlyoutPage has no Detail page set.";
            _logger.LogWarning(error);
            return Result.Fail(ErrorCode.InvalidState, error);
        }

        var isFlyoutInSplitViewMode = IsFlyoutInSplitViewMode(flyoutPage);
        var isFlyoutDetailRootRequest = parameters?.ContainsKey(NavigationHints.IsFlyoutDetailRoot) == true;
        if (isFlyoutInSplitViewMode && isFlyoutDetailRootRequest)
        {
            return await HandleFlyoutMenuNavigation(flyoutPage, detail, newPages);
        }

        var navigationPage = NavigationHelper.FindNavigationPage(detail);
        if (navigationPage is null)
        {
            const string error = "Cannot navigate within FlyoutPage detail without a NavigationPage.";
            _logger.LogWarning(error);
            return Result.Fail(ErrorCode.NotSupported, error);
        }

        return await _recursiveNavigationHandler(detail, newPages, parameters, animated);
    }

    private static async Task<IResult> HandleFlyoutMenuNavigation(FlyoutPage flyoutPage, Page detail, Page[] newPages)
    {
        // Store original Detail on first navigation
        _ = OriginalDetails.TryAdd(flyoutPage, detail);

        var targetPage = newPages.First();
        var originalDetail = OriginalDetails[flyoutPage];

        // Check if navigating back to original Detail
        var requestedViewModelName = PageHelper.ToViewModelName(targetPage.GetType());
        var originalViewModelName = originalDetail.BindingContext?.GetType().Name;
        var isNavigatingToOriginal = requestedViewModelName == originalViewModelName;

        if (isNavigatingToOriginal)
        {
            flyoutPage.Detail = originalDetail;
            return Result.Ok();
        }
        
        // Workaround for MAUI bug: https://github.com/dotnet/maui/issues/22116
        if (flyoutPage.Detail is NavigationPage navPage)
        {
            await navPage.PopToRootAsync(animated: false);

            var pageToInsert = isNavigatingToOriginal ? originalDetail : targetPage;
            navPage.Navigation.InsertPageBefore(pageToInsert, navPage.RootPage);
            await navPage.PopAsync(animated: false);
        }
        else
        {
            // Detail is not a NavigationPage - wrap in one (only happens once)
            var pageToWrap = isNavigatingToOriginal ? originalDetail : targetPage;
            navPage = new NavigationPage(pageToWrap);
            flyoutPage.Detail = navPage;
        }
        
        // Push remaining pages if any
        foreach (var nextPage in newPages.Skip(1))
        {
            await navPage.PushAsync(nextPage, animated: false);
        }

        return Result.Ok();
    }
    
    private static bool IsFlyoutInSplitViewMode(FlyoutPage flyoutPage)
    {
        var behavior = flyoutPage.FlyoutLayoutBehavior;
    
        // Explicitly set to Split
        if (behavior == FlyoutLayoutBehavior.Split)
            return true;

        // For Default behavior, check if flyout is visible while IsPresented is false
        // In split mode, both panes are always visible
        // In overlay mode, flyout is only visible when IsPresented is true
        return flyoutPage is { IsPresented: false, Flyout.IsVisible: true };
    }
}