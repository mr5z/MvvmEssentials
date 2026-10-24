using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Extensions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Pages;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using iOSPage = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page;

namespace Nkraft.MvvmEssentials.Services;

public interface IModalService
{
    /// <summary>
    /// Presents the page named by <paramref name="modalName"/> on top of the window's modal stack.
    /// The name must resolve to exactly one page.
    /// <para>
    /// The page is hosted in a <see cref="NavigationPage"/> unless it sets
    /// <c>Modal.WithNavigation="False"</c>. That host, added for the top bar, is also what lets the modal's view
    /// models push and pop pages inside it (<c>ModalViewModel.PushAsync</c>).
    /// </para>
    /// <para>
    /// On Android, the Back button goes to the previous page inside the modal, and on its first page is routed to a
    /// <see cref="ViewModels.ModalViewModel{TResult}"/>'s <c>Dismiss()</c>, so its <c>CanDismissAsync</c> can keep
    /// the modal open.
    /// </para>
    /// </summary>
    Task<IResult> PresentAsync(string modalName, INavigationParameters? parameters = null, bool animated = true);

    /// <summary>
    /// Dismisses the top-most modal page.
    /// </summary>
    Task<IResult> DismissAsync(bool animated = true);

    /// <summary>
    /// Dismisses every modal page; only the last one is animated.
    /// </summary>
    Task<IResult> DismissAllAsync(bool animated = true);
}

internal sealed class ModalService(
    ILogger<ModalService> logger,
    IPageFactory pageFactory,
    IApplicationContext context,
    IDispatcher dispatcher) : IModalService, IStackPresenter
{
    private readonly ILogger<ModalService> _logger = logger;
    private readonly IPageFactory _pageFactory = pageFactory;
    private readonly IApplicationContext _context = context;
    private readonly IDispatcher _dispatcher = dispatcher;
    private readonly SemaphoreSlim _gate = new(1, 1);

    // The modals this service presented and that are still on screen, keyed by the page pushed with PushModalAsync
    private readonly Dictionary<Page, Presentation> _presentations = [];

    // The modal this service is removing right now. Its pop is let through; any other pop
    // (Android Back) is routed to the view model first.
    private Page? _pagePoppedByService;

    // Back exists only on Android. On iOS, MAUI raises ModalPopping after a swiped-away sheet is already
    // gone (ControlsModalWrapper.DidDismiss), so cancelling there would leave the modal stack out of sync.
    // Not using preprocessor directive for testability purpose.
    internal bool InterceptsBackNavigation { get; init; } = OperatingSystem.IsAndroid();

    async Task<IResult> IModalService.PresentAsync(string modalName, INavigationParameters? parameters, bool animated)
    {
        if (TryGetWindow(out var window) == false)
            return WindowNotFound();

        if (ResolveSinglePage(modalName, out var pageInfo) is { } invalidName)
            return invalidName;

        await _gate.WaitAsync();
        using var lease = new PageLease(_pageFactory, _logger);
        Page? host = null;

        try
        {
            // PresentAsync<TViewModel, TResult> passes a presentation that the caller awaits; otherwise nobody does
            var modalParameters = parameters ?? new NavigationParameters();
            if (modalParameters.TryGetValue<Presentation>(NavigationHints.Presentation, out var presentation) == false)
            {
                presentation = new Presentation();
                modalParameters[NavigationHints.Presentation] = presentation;
            }

            var modalPage = lease.Create(pageInfo, modalParameters);
            host = WrapIfNeeded(modalPage);
            presentation.Open(this, modalPage, host);

            ObserveWindow(window);
            _presentations[host] = presentation;
            await window.Navigation.PushModalAsync(host, animated);
            lease.Commit(host);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            if (host is not null)
            {
                _presentations.Remove(host);
            }

            const string message = "An error occurred while trying to present '{ModalName}'.";
            _logger.LogError(ex, message, modalName);
            return Result.Fail(ErrorCode.General, ex, message, modalName);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<IResult> IModalService.DismissAsync(bool animated)
    {
        if (TryGetWindow(out var window) == false)
            return WindowNotFound();

        var navigation = window.Navigation;

        await _gate.WaitAsync();

        try
        {
            if (navigation.ModalStack.Count == 0)
            {
                const string message = "No modal page to dismiss.";
                _logger.LogInformation(message);
                return Result.Fail(ErrorCode.NotHandled, message);
            }

            await PopTopModalAsync(navigation, animated);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to dismiss modal page.";
            _logger.LogError(ex, message);
            return Result.Fail(ErrorCode.General, ex, message);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<IResult> IModalService.DismissAllAsync(bool animated)
    {
        if (TryGetWindow(out var window) == false)
            return WindowNotFound();

        var navigation = window.Navigation;

        await _gate.WaitAsync();

        try
        {
            while (navigation.ModalStack.Count > 0)
            {
                // Only animate the last pop; animating each in sequence is slow and janky
                var isLast = navigation.ModalStack.Count == 1;
                await PopTopModalAsync(navigation, animated: animated && isLast);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to dismiss all modal pages.";
            _logger.LogError(ex, message);
            return Result.Fail(ErrorCode.General, ex, message);
        }
        finally
        {
            _gate.Release();
        }
    }

    // -----------------------------------------------------------------------
    // IStackPresenter: what a presentation asks for (DismissAsync is IPresenter's)
    // -----------------------------------------------------------------------

    Task<IResult> IStackPresenter.PushAsync(
        Presentation presentation, string pageName, INavigationParameters? parameters, bool animated)
        => PushInsideAsync(presentation, pageName, parameters, animated, replace: false);

    Task<IResult> IStackPresenter.ReplaceAsync(
        Presentation presentation, string pageName, INavigationParameters? parameters, bool animated)
        => PushInsideAsync(presentation, pageName, parameters, animated, replace: true);

    // Replace pushes first and removes the covered page after: MAUI can't remove the page on screen, and the new page
    // slides in over it as a push would.
    private async Task<IResult> PushInsideAsync(
        Presentation presentation, string pageName, INavigationParameters? parameters, bool animated, bool replace)
    {
        if (ResolveSinglePage(pageName, out var pageInfo) is { } invalidName)
            return invalidName;

        await _gate.WaitAsync();
        using var lease = new PageLease(_pageFactory, _logger);

        try
        {
            if (FindStack(presentation, out var stack) is { } unavailable)
                return unavailable;

            // A copy, so the caller's parameters aren't changed; the page joins the same presentation
            var pageParameters = new NavigationParameters();
            foreach (var (key, value) in parameters ?? new NavigationParameters())
            {
                pageParameters.Add(key, value);
            }
            pageParameters[NavigationHints.Presentation] = presentation;

            var covered = stack.CurrentPage;
            await stack.PushAsync(lease.Create(pageInfo, pageParameters), animated);
            lease.Commit(presentation.Host!);

            if (replace)
            {
                // Out of the tree, so PageFactory releases its scope
                stack.Navigation.RemovePage(covered);
                if (ReferenceEquals(covered, presentation.FirstPage))
                {
                    presentation.ReplaceFirstPage(stack.RootPage);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to push '{PageName}' inside the modal.";
            _logger.LogError(ex, message, pageName);
            return Result.Fail(ErrorCode.General, ex, message, pageName);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<IResult> IStackPresenter.PopAsync(Presentation presentation, bool animated)
    {
        await _gate.WaitAsync();

        try
        {
            if (FindStack(presentation, out var stack) is { } unavailable)
                return unavailable;

            if (stack.Navigation.NavigationStack.Count <= 1)
            {
                const string message = "Already on the modal's first page; leave the modal with Dismiss.";
                _logger.LogInformation(message);
                return Result.Fail(ErrorCode.NotHandled, message);
            }

            await stack.PopAsync(animated);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to go back inside the modal.";
            _logger.LogError(ex, message);
            return Result.Fail(ErrorCode.General, ex, message);
        }
        finally
        {
            _gate.Release();
        }
    }

    async Task<IResult> IPresenter.DismissAsync(Presentation presentation)
    {
        if (TryGetWindow(out var window) == false)
            return WindowNotFound();

        var navigation = window.Navigation;

        await _gate.WaitAsync();

        try
        {
            if (presentation.IsClosed)
            {
                const string message = "The modal has already closed.";
                _logger.LogInformation(message);
                return Result.Fail(ErrorCode.NotHandled, message);
            }

            // MAUI can only pop the top modal
            if (navigation.ModalStack.Count == 0 || ReferenceEquals(navigation.ModalStack[^1], presentation.Host) == false)
            {
                const string message = "Another modal is on top of this one; close that one first.";
                _logger.LogWarning(message);
                return Result.Fail(ErrorCode.InvalidState, message);
            }

            await PopTopModalAsync(navigation, animated: true);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to dismiss modal page.";
            _logger.LogError(ex, message);
            return Result.Fail(ErrorCode.General, ex, message);
        }
        finally
        {
            _gate.Release();
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    internal static Page WrapIfNeeded(Page page)
    {
        if (Modal.GetWithNavigation(page) == false || page is NavigationPage or TabbedPage or FlyoutPage)
            return page;

        var wrapper = new NavigationPage(page);

        if (page.IsSet(iOSPage.ModalPresentationStyleProperty))
            wrapper.SetValue(iOSPage.ModalPresentationStyleProperty, page.GetValue(iOSPage.ModalPresentationStyleProperty));

        return wrapper;
    }

    private IResult? ResolveSinglePage(string name, out PageInfo pageInfo)
    {
        pageInfo = null!;
        PageInfo[] pageInfoList;

        try
        {
            pageInfoList = _pageFactory.GetPageTypesFromPath(name);
        }
        catch (Exception ex)
        {
            const string message = "An error occurred when trying to decode page name '{PageName}'.";
            _logger.LogError(ex, message, name);
            return Result.Fail(ErrorCode.InvalidParameter, ex, message, name);
        }

        if (pageInfoList.Length != 1)
        {
            const string message = "Page name '{PageName}' must resolve to exactly one page.";
            _logger.LogWarning(message, name);
            return Result.Fail(ErrorCode.NotSupported, message, name);
        }

        pageInfo = pageInfoList[0];
        return null;
    }

    // The NavigationPage hosting the presentation's pages, which exists unless Modal.WithNavigation="False"
    private IResult? FindStack(Presentation presentation, out NavigationPage stack)
    {
        stack = null!;

        if (presentation.IsClosed || presentation.Host is null)
        {
            const string message = "The modal has already closed.";
            _logger.LogInformation(message);
            return Result.Fail(ErrorCode.InvalidState, message);
        }

        if (NavigationHelper.FindNavigationPage(presentation.Host) is not { } navigationPage)
        {
            const string message = "The modal is a single page (its first page sets Modal.WithNavigation=\"False\"), " +
                "so there is no NavigationPage to push or pop on.";
            _logger.LogWarning(message);
            return Result.Fail(ErrorCode.NotSupported, message);
        }

        stack = navigationPage;
        return null;
    }

    private async Task PopTopModalAsync(INavigation navigation, bool animated)
    {
        _pagePoppedByService = navigation.ModalStack[^1];
        try
        {
            await navigation.PopModalAsync(animated);
        }
        finally
        {
            _pagePoppedByService = null;
        }
    }

    // -----------------------------------------------------------------------
    // Window events: MAUI telling us a modal went away
    // -----------------------------------------------------------------------

    private void ObserveWindow(Window window)
    {
        // Remove first, so presenting several modals still subscribes once per window.
        window.ModalPopped -= OnModalPopped;
        window.ModalPopped += OnModalPopped;
        window.PropertyChanging -= OnWindowPropertyChanging;
        window.PropertyChanging += OnWindowPropertyChanging;

        if (InterceptsBackNavigation == false)
            return;

        window.ModalPopping -= OnModalPopping;
        window.ModalPopping += OnModalPopping;
    }

    // Every removal through MAUI's PopModalAsync: Dismiss, DismissAll, Android Back, a direct PopModalAsync, and
    // the iOS sheet swipe-down (ControlsModalWrapper.DidDismiss pops through the window).
    private void OnModalPopped(object? sender, ModalPoppedEventArgs e)
    {
        if (_presentations.Remove(e.Modal, out var presentation))
        {
            presentation.OnClosed();
        }
    }

    // Replacing the window's page (absolute navigation) makes MAUI clear its modal stack without ModalPopped.
    private void OnWindowPropertyChanging(object? sender, Microsoft.Maui.Controls.PropertyChangingEventArgs e)
    {
        if (e.PropertyName != Window.PageProperty.PropertyName)
            return;

        var closed = _presentations.Values.ToList();
        _presentations.Clear();
        closed.ForEach(presentation => presentation.OnClosed());
    }

    private void OnModalPopping(object? sender, ModalPoppingEventArgs e)
    {
        if (ReferenceEquals(e.Modal, _pagePoppedByService))
            return;

        // Back with pages pushed inside the modal goes to the previous page, like any NavigationPage
        if (_presentations.TryGetValue(e.Modal, out var presentation)
            && NavigationHelper.FindNavigationPage(e.Modal) is { Navigation.NavigationStack.Count: > 1 })
        {
            e.Cancel = true;
            _dispatcher.DispatchAsync(async () => await presentation.PopAsync(animated: true)).FireAndForget(ex =>
            {
                const string message = "Going back inside the modal after Back failed.";
                _logger.LogError(ex, message);
            });
            return;
        }

        var page = presentation?.FirstPage ?? (e.Modal is NavigationPage host ? host.RootPage : e.Modal);
        if (page.BindingContext is not IDismissible dismissible)
            return;

        e.Cancel = true;
        _dispatcher.DispatchAsync(async () => await dismissible.Dismiss()).FireAndForget(ex =>
        {
            const string message = "Dismissing modal '{PageName}' after Back failed.";
            _logger.LogError(ex, message, page.GetType().Name);
        });
    }

    private bool TryGetWindow([NotNullWhen(true)] out Window? window)
    {
        window = _context.Windows.Count > 0 ? _context.Windows[0] : null;
        return window is not null;
    }

    private IResult WindowNotFound()
    {
        const string message = "No window is available for modal navigation.";
        _logger.LogWarning(message);
        return Result.Fail(ErrorCode.InvalidState, message);
    }
}
