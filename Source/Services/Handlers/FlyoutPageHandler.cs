using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class FlyoutPageHandler(
    ILogger logger,
    Dictionary<FlyoutPage, Page> originalDetails,
    Func<Page?, Page[], bool, Task<IResult>> recursiveNavigationHandler) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;
    private readonly Dictionary<FlyoutPage, Page> _originalDetails = originalDetails;
    private readonly Func<Page?, Page[], bool, Task<IResult>> _recursiveNavigationHandler = recursiveNavigationHandler;

    public bool CanHandle(Page? page) => page is FlyoutPage;

    public async Task<IResult> HandleAsync(Page page, Page[] newPages, bool animated)
    {
        var flyoutPage = (FlyoutPage)page;
        var detail = flyoutPage.Detail;
        
        if (detail is null)
        {
            const string error = "FlyoutPage has no Detail page set.";
            _logger.LogWarning(error);
            return Result.Fail(ErrorCode.InvalidState, error);
        }

        if (flyoutPage.IsPresented)
        {
            return await HandleFlyoutMenuNavigation(flyoutPage, detail, newPages);
        }

        var navigationPage = FindNavigationPage(detail);
        if (navigationPage is null)
        {
            const string error = "Cannot navigate within FlyoutPage detail without a NavigationPage.";
            _logger.LogWarning(error);
            return Result.Fail(ErrorCode.NotSupported, error);
        }

        return await _recursiveNavigationHandler(detail, newPages, animated);
    }

    private async Task<IResult> HandleFlyoutMenuNavigation(FlyoutPage flyoutPage, Page detail, Page[] newPages)
    {
        // Store original Detail on first navigation
        _originalDetails.TryAdd(flyoutPage, detail);

        var targetPage = newPages.First();
        var originalDetail = _originalDetails[flyoutPage];

        // Check if navigating back to original Detail
        var requestedViewModelName = PageHelper.ToViewModelName(targetPage.GetType());
        var originalViewModelName = originalDetail.BindingContext?.GetType()?.Name;
        var isNavigatingToOriginal = requestedViewModelName == originalViewModelName;

        // Workaround for MAUI bug: https://github.com/dotnet/maui/issues/22116
        if (flyoutPage.Detail is NavigationPage navPage)
        {
            await navPage.PopToRootAsync(false);

            var pageToInsert = isNavigatingToOriginal ? originalDetail : targetPage;
            navPage.Navigation.InsertPageBefore(pageToInsert, navPage.RootPage);
            await navPage.PopAsync(false);
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
            await navPage.PushAsync(nextPage, false);
        }

        return Result.Ok();
    }

    private static NavigationPage? FindNavigationPage(Page page)
    {
        return page switch
        {
            NavigationPage navPage => navPage,
            FlyoutPage flyout => FindNavigationPage(flyout.Detail),
            TabbedPage { CurrentPage: not null } tabbed => FindNavigationPage(tabbed.CurrentPage),
            _ => null
        };
    }
}