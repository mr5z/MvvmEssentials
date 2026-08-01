using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Handlers;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.Services;

public interface INavigationService
{
    /// <summary>
    /// Navigates to the page(s) described by <paramref name="path"/>.
    /// <para>
    /// - An absolute path (starting with '/') replaces the current root page.
    /// </para>
    /// <para>
    /// - A relative path navigates within the current page context.
    /// </para>
    /// </summary>
    /// <param name="path">The navigation path, composed of page names and optional query parameters.</param>
    /// <param name="parameters">Optional navigation parameters to pass to the target view model.</param>
    /// <param name="animated">Whether to animate the navigation transition.</param>
    /// <returns>An <see cref="IResult"/> indicating success or failure of the navigation operation.</returns>
    Task<IResult> NavigateAsync(string path, INavigationParameters? parameters = null, bool animated = true);

    /// <summary>
    /// Navigates back to the previous page in the navigation stack.
    /// <para>
    /// - If the current page is within a <see cref="NavigationPage"/>, this will pop the top page from the navigation stack.
    /// </para>
    /// <para>
    /// - If not within a <see cref="NavigationPage"/>, this will call <see cref="Page.SendBackButtonPressed()"/> on the current page.
    /// </para>
    /// </summary>
    /// <param name="animated">Whether to animate the navigation transition.</param>
    /// <returns>An <see cref="IResult"/> indicating success or failure of the navigation operation.</returns>
    Task<IResult> NavigateBackAsync(bool animated = true);

    /// <summary>
    /// Navigates to the root page of the navigation stack.
    /// <para>
    /// - If the root page implements <see cref="IRootPageNavigated"/>, the corresponding event will be delivered.
    /// </para>
    /// - Only supported when the current page is a <see cref="NavigationPage"/> or the current tab of a <see cref="TabbedPage"/> is a <see cref="NavigationPage"/>.
    /// </summary>
    /// <param name="parameters">Optional navigation parameters to pass to the root page's view model.</param>
    /// <param name="animated">Whether to animate the navigation transition.</param>
    /// <returns>An <see cref="IResult"/> indicating success or failure of the navigation operation.</returns>
    Task<IResult> NavigateToRootAsync(INavigationParameters? parameters = null, bool animated = true);
}

internal sealed class NavigationService(
    ILogger<NavigationService> logger,
    IPageFactory pageFactory,
    IApplicationContext applicationContext) : INavigationService
{
    private readonly ILogger<NavigationService> _logger = logger;
    private readonly IPageFactory _pageFactory = pageFactory;
    private readonly IApplicationContext _applicationContext = applicationContext;

    // Handle support nested NavigationPage if needed in the future.
    async Task<IResult> INavigationService.NavigateAsync(string path, INavigationParameters? parameters, bool animated)
    {
        PageInfo[] pageInfoList;

        try
        {
            pageInfoList = _pageFactory.GetPageTypesFromPath<Page>(path);
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to fetch page information.";
            _logger.LogError(ex, message);
            return Result.Fail(ErrorCode.InvalidState, message);
        }

        if (pageInfoList.Length == 0)
        {
            const string message = "No valid pages found in the navigation path.";
            _logger.LogWarning(message);
            return Result.Fail(ErrorCode.InvalidState, message);
        }

        var request = new NavigationRequest(pageInfoList, parameters ?? new NavigationParameters(), _pageFactory);

        try
        {
            var replaceCurrentPage = path.StartsWith('/');

            if (replaceCurrentPage)
            {
                // Build the page stack
                var pages = request.MaterializeAll();
                if (pages.Count == 0)
                {
                    const string message = "No valid pages to navigate to.";
                    _logger.LogWarning(message);
                    return Result.Fail(ErrorCode.InvalidState, message);
                }

                var firstPage = pages[0];
                var mainPageResult = await BuildRootPageAsync(firstPage, pages, animated);
                if (mainPageResult.TryGetValue(out var mainPage))
                {
                    _applicationContext.MainPage = mainPage;
                }
                else
                {
                    return mainPageResult;
                }
            }
            else
            {
                var currentPage = _applicationContext.MainPage;
                if (currentPage is null)
                {
                    const string message = "Current page is null.";
                    _logger.LogWarning(message);
                    return Result.Fail(ErrorCode.InvalidState, message);
                }
                return await HandleContextualNavigationAsync(currentPage, request, animated);
            }
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to perform page navigation (Path: {Path}).";
            _logger.LogError(ex, message, path);
            return Result.Fail(ErrorCode.Unknown, message, path);
        }
        finally
        {
            if (request.ReleaseUnreachablePages(root: _applicationContext.MainPage) is {} ex)
            {
                const string message = "One or more page scopes failed to dispose during navigation (Path: {Path}).";
                _logger.LogWarning(ex, message, path);
            }
        }

        return Result.Ok();
    }

    async Task<IResult> INavigationService.NavigateBackAsync(bool animated)
    {
        if (TryGetCurrentPage(out var currentPage) == false)
        {
            const string message = "Main page is not set for current window.";
            _logger.LogWarning(message);
            return Result.Fail(ErrorCode.InvalidState, message);
        }

        var navigationPage = NavigationHelper.FindNavigationPage(currentPage);
        if (navigationPage is not null)
        {
            var isRootPage = navigationPage.Navigation.NavigationStack.Count <= 1;
            if (isRootPage)
            {
                const string message = "Already at the root of the navigation stack; nothing to navigate back to.";
                _logger.LogInformation(message);
                return Result.Fail(ErrorCode.NotHandled, message);
            }

            var previousPage = await navigationPage.PopAsync(animated);
            if (previousPage is null)
            {
                const string message = "Popped page returns null.";
                _logger.LogWarning(message);
                return Result.Fail(ErrorCode.InvalidState, message);
            }

            return Result.Ok();
        }

        var navigatedBack = currentPage.SendBackButtonPressed();
        if (navigatedBack)
            return Result.Ok();
        
        // Nobody handled it and there's no stack to pop, so it's not really "canceled"
        // it's just not our problem anymore. Android might exit the app here, iOS
        // probably just does nothing. Either way, not up to us.
        const string messageInfo = "Back navigation was not handled within the app; platform default behavior may apply.";
        _logger.LogInformation(messageInfo);
        return Result.Fail(ErrorCode.NotHandled, messageInfo);
    }

    async Task<IResult> INavigationService.NavigateToRootAsync(INavigationParameters? parameters, bool animated)
    {
        if (TryGetCurrentPage(out var currentPage) == false)
        {
            const string message = "Main page is not set for current window.";
            _logger.LogWarning(message);
            return Result.Fail(ErrorCode.InvalidState, message);
        }

        var navigationPage = NavigationHelper.FindNavigationPage(currentPage);
        if (navigationPage is null)
        {
            const string message = "Root navigation is only supported within NavigationPage.";
            _logger.LogWarning(message);
            return Result.Fail(ErrorCode.NotSupported, message);
        }

        if (navigationPage.Navigation.NavigationStack.Count <= 1)
        {
            const string message = "No page to navigate back to.";
            // I can't really decide whether the log should be informational or a warning
            // and now we have inconsistency since the other is informational
            _logger.LogWarning(message);
            return Result.Fail(ErrorCode.NotHandled, message);
        }

        try
        {
            await navigationPage.PopToRootAsync(animated);
            var rootPage = navigationPage.CurrentPage;
            if (rootPage.BindingContext is IRootPageNavigated vm)
            {
                vm.OnNavigatedToRoot(parameters ?? new NavigationParameters());
                await vm.OnNavigatedToRootAsync(parameters ?? new NavigationParameters());
            }
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to navigate to root.";
            _logger.LogError(ex, message);
            return Result.Fail(ErrorCode.General, message);
        }

        return Result.Ok();
    }

    private async Task<Result<Page>> BuildRootPageAsync(Page firstPage, IReadOnlyCollection<Page> pages, bool animated)
    {
        // If there's only one page or no pages to push, just return as-is
        if (pages.Count <= 1)
            return Result.Ok(firstPage);

        var navigationPage = NavigationHelper.FindNavigationPage(firstPage);
        if (navigationPage is not null)
        {
            await PushPagesAsync(navigationPage, pages.Skip(1), animated);
            return Result.Ok(firstPage);
        }

        const string message = "No NavigationPage found in the page hierarchy and multiple pages were requested.";
        _logger.LogWarning(message);
        return Result.Fail<Page>(ErrorCode.NotSupported, message);
    }

    private async Task<IResult> HandleContextualNavigationAsync(Page? currentPage, NavigationRequest request, bool animated)
    {
        var handlers = new IPageNavigationHandler[]
        {
            new NavigationPageHandler(_logger),
            new TabbedPageHandler(_logger),
            new FlyoutPageHandler(_logger),
            new UnsupportedPageHandler(_logger)
        };

        foreach (var handler in handlers)
        {
            if (handler.CanHandle(currentPage) == false)
                continue;

            var navigationContext = await handler.HandleAsync(currentPage!, request, animated);
            if (navigationContext.TryGetValue(out var context) && context.Action == NavigationAction.ContinueInto)
                return await HandleContextualNavigationAsync(context.NextPage, request, animated);
            
            return navigationContext;
        }

        return Result.Fail(ErrorCode.NotSupported, "No handler found for current page.");
    }

    private static async Task PushPagesAsync(NavigationPage navigationPage, IEnumerable<Page> newPages, bool animated)
    {
        foreach (var page in newPages)
        {
            await navigationPage.PushAsync(page, animated);
        }
    }

    private bool TryGetCurrentPage([NotNullWhen(true)] out Page? page)
    {
        if (_applicationContext.Windows.Count > 0 && _applicationContext.Windows[0].Page is { } currentPage)
        {
            page = currentPage;
            return true;
        }

        page = null;
        return false;
    }
}
