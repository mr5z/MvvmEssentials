using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.ViewModels;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.Services.Handlers;

internal class FlyoutPageHandler(ILogger logger) : IPageNavigationHandler
{
    private readonly ILogger _logger = logger;

    bool IPageNavigationHandler.CanHandle(Page? page) => page is FlyoutPage;

    async Task<Result<NavigationContext>> IPageNavigationHandler.HandleAsync(Page page, NavigationRequest request, bool animated)
    {
        var flyoutPage = (FlyoutPage)page;
        var detail = flyoutPage.Detail;

        if (detail is null)
        {
            const string error = "FlyoutPage has no Detail page set.";
            _logger.LogWarning(error);
            return Result.Fail<NavigationContext>(ErrorCode.InvalidState, error);
        }

        var isFlyoutDetailRootRequest = request.Parameters.ContainsKey(NavigationHints.IsFlyoutDetailRoot);
        if (isFlyoutDetailRootRequest)
        {
            return await HandleFlyoutMenuNavigation(flyoutPage, detail, request);
        }

        return Result.Ok(NavigationContext.Into(detail));
    }

    private async Task<Result<NavigationContext>> HandleFlyoutMenuNavigation(FlyoutPage flyoutPage, Page detail, NavigationRequest request)
    {
        var detailHost = flyoutPage.BindingContext as IInitialDetail;
        // Store original Detail on first navigation due to weird design of MAUI
        // I.e., if we don't, initial BindingContext will not match the injected VM from Flyout's VM and that's a recipe
        // for a disaztah
        detailHost?.DetailPage ??= detail;
        
        if (detailHost?.DetailPage is not { } detailPage)
        {
            const string error = "The initial detail page was not found. The navigation cannot proceed.";
            _logger.LogWarning(error);
            return Result.Fail<NavigationContext>(ErrorCode.InvalidState, error);
        }

        // Check if navigating back to initial Detail
        var requestedViewModelName = PageHelper.ToViewModelName(request.Pages[0].PageType);
        var initialViewModelName = detailPage.BindingContext?.GetType().Name;
        var isNavigatingToInitial = requestedViewModelName == initialViewModelName;

        if (isNavigatingToInitial)
        {
            flyoutPage.Detail = detailPage;
            return Result.Ok(NavigationContext.Complete());
        }

        var targetPage = request.Materialize(request.Pages[0]);
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
        foreach (var nextPage in request.Pages.Skip(1))
        {
            await navPage.PushAsync(request.Materialize(nextPage), animated: false);
        }

        return Result.Ok(NavigationContext.Complete());
    }
}
