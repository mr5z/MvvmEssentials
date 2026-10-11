using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Extensions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Pages;
using Nkraft.MvvmEssentials.Services.Modals;
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
    /// <c>Modal.WithNavigation="False"</c>.
    /// </para>
    /// <para>
    /// On Android, the Back button is routed to a <see cref="ViewModels.ModalViewModel{TResult}"/>'s
    /// <c>Dismiss()</c>, so its <c>CanDismissAsync</c> can keep the modal open.
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
    IDispatcher dispatcher) : IModalService
{
    private readonly ILogger<ModalService> _logger = logger;
    private readonly IPageFactory _pageFactory = pageFactory;
    private readonly IApplicationContext _context = context;
    private readonly IDispatcher _dispatcher = dispatcher;
    private readonly SemaphoreSlim _gate = new(1, 1);

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

        PageInfo[] pageInfoList;

        try
        {
            pageInfoList = _pageFactory.GetPageTypesFromPath(modalName);
        }
        catch (Exception ex)
        {
            const string message = "An error occurred when trying to decode modal name '{ModalName}'.";
            _logger.LogError(ex, message, modalName);
            return Result.Fail(ErrorCode.InvalidParameter, message, modalName);
        }

        if (pageInfoList.Length != 1)
        {
            const string message = "Modal name '{ModalName}' must resolve to exactly one page.";
            _logger.LogWarning(message, modalName);
            return Result.Fail(ErrorCode.NotSupported, message, modalName);
        }

        await _gate.WaitAsync();
        using var lease = new PageLease(_pageFactory, _logger);

        try
        {
            var modalPage = lease.Create(pageInfoList[0], parameters);
            var presentedPage = WrapIfNeeded(modalPage);
            ObserveModalPopping(window);
            await window.Navigation.PushModalAsync(presentedPage, animated);
            lease.Commit(presentedPage);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            const string message = "An error occurred while trying to present '{ModalName}'.";
            _logger.LogError(ex, message, modalName);
            return Result.Fail(ErrorCode.General, message, modalName);
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
            return Result.Fail(ErrorCode.General, message);
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
            return Result.Fail(ErrorCode.General, message);
        }
        finally
        {
            _gate.Release();
        }
    }

    internal static Page WrapIfNeeded(Page page)
    {
        if (Modal.GetWithNavigation(page) == false || page is NavigationPage or TabbedPage or FlyoutPage)
            return page;

        var wrapper = new NavigationPage(page);

        if (page.IsSet(iOSPage.ModalPresentationStyleProperty))
            wrapper.SetValue(iOSPage.ModalPresentationStyleProperty, page.GetValue(iOSPage.ModalPresentationStyleProperty));

        return wrapper;
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

    private void ObserveModalPopping(Window window)
    {
        if (InterceptsBackNavigation == false)
            return;

        // Remove first, so presenting several modals still subscribes once per window.
        window.ModalPopping -= OnModalPopping;
        window.ModalPopping += OnModalPopping;
    }

    private void OnModalPopping(object? sender, ModalPoppingEventArgs e)
    {
        if (ReferenceEquals(e.Modal, _pagePoppedByService))
            return;

        var page = e.Modal is NavigationPage host ? host.RootPage : e.Modal;
        if (page.BindingContext is not IModalDismissible dismissible)
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