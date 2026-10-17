using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services.Modals;

/// <summary>
/// The caller's side of a modal presented with <c>PresentAsync&lt;TViewModel, TResult&gt;</c>: completes the
/// caller's task with a result, or cancels it. Lets view models that can't derive from
/// <see cref="ViewModels.ModalViewModel{TResult}"/> (e.g. <see cref="ViewModels.WizardHostViewModel{TState}"/>)
/// share its dismissal rules.
/// </summary>
internal sealed class ModalSession<TResult>(IModalService modalService)
{
    private readonly IModalService _modalService = modalService;

    private TaskCompletionSource<TResult>? _completion;

    // Set while this session is popping the modal, so the resulting unload isn't mistaken for a system dismissal
    private bool _isDismissing;

    // The cancel dismissal in progress, shared by concurrent callers (e.g. Back pressed twice during a prompt)
    private Task<bool>? _pendingDismiss;

    public void Attach(INavigationParameters parameters)
    {
        if (parameters.TryGetValue<TaskCompletionSource<TResult>>(NavigationHints.ModalCompletionParam, out var completion))
        {
            _completion = completion;
        }
    }

    /// <summary>
    /// The modal's view model was disposed. Without a <c>Dismiss</c> in progress, the modal was removed by the
    /// system (Android Back, iOS sheet swipe-down), so the caller receives <c>Cancelled</c>.
    /// </summary>
    public void NotifyDisposed()
    {
        if (_isDismissing == false)
        {
            _completion?.TrySetCanceled();
        }
    }

    /// <summary>
    /// Dismisses without a result after <paramref name="canDismiss"/> agrees.
    /// Calls made while a dismissal is in progress share its outcome.
    /// </summary>
    public Task<bool> DismissAsync(Func<Task<bool>> canDismiss)
    {
        if (_pendingDismiss is { IsCompleted: false } pending)
            return pending;

        return _pendingDismiss = DismissWithoutResultAsync(canDismiss);
    }

    public async Task<IResult> DismissAsync(TResult result)
    {
        var modalResult = await DismissModalAsync();
        if (modalResult.IsSuccess)
        {
            _completion?.TrySetResult(result);
        }
        else
        {
            _completion?.TrySetException(modalResult.Exception ?? new Exception(modalResult.ErrorMessage));
        }

        return modalResult;
    }

    private async Task<bool> DismissWithoutResultAsync(Func<Task<bool>> canDismiss)
    {
        if (await canDismiss() == false)
            return false;

        var result = await DismissModalAsync();
        if (result.IsSuccess)
        {
            _completion?.TrySetCanceled();
        }
        return result.IsSuccess;
    }

    private async Task<IResult> DismissModalAsync()
    {
        _isDismissing = true;

        try
        {
            return await _modalService.DismissAsync();
        }
        finally
        {
            _isDismissing = false;
        }
    }
}
