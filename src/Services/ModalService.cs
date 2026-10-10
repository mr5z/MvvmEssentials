using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Pages;

namespace Nkraft.MvvmEssentials.Services;

public interface IModalService
{
    /// <summary>
    /// Presents the page named by <paramref name="modalName"/> on top of the window's modal stack.
    /// The name must resolve to exactly one page.
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
    IApplicationContext context) : IModalService
{
    private readonly ILogger<ModalService> _logger = logger;
    private readonly IPageFactory _pageFactory = pageFactory;
    private readonly IApplicationContext _context = context;
    private readonly SemaphoreSlim _gate = new(1, 1);

    async Task<IResult> IModalService.PresentAsync(string modalName, INavigationParameters? parameters, bool animated)
    {
        if (TryGetNavigation(out var navigation) == false)
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
            await navigation.PushModalAsync(modalPage, animated);
            lease.Commit(modalPage);
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
        if (TryGetNavigation(out var navigation) == false)
            return WindowNotFound();

        await _gate.WaitAsync();

        try
        {
            if (navigation.ModalStack.Count == 0)
            {
                const string message = "No modal page to dismiss.";
                _logger.LogInformation(message);
                return Result.Fail(ErrorCode.NotHandled, message);
            }

            await navigation.PopModalAsync(animated);
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
        if (TryGetNavigation(out var navigation) == false)
            return WindowNotFound();

        await _gate.WaitAsync();

        try
        {
            while (navigation.ModalStack.Count > 0)
            {
                // Only animate the last pop; animating each in sequence is slow and janky
                var isLast = navigation.ModalStack.Count == 1;
                await navigation.PopModalAsync(animated: animated && isLast);
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

    private bool TryGetNavigation([NotNullWhen(true)] out INavigation? navigation)
    {
        navigation = _context.Windows.Count > 0 ? _context.Windows[0].Navigation : null;
        return navigation is not null;
    }

    private IResult WindowNotFound()
    {
        const string message = "No window is available for modal navigation.";
        _logger.LogWarning(message);
        return Result.Fail(ErrorCode.InvalidState, message);
    }
}