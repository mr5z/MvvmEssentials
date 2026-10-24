using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Modals;

/// <summary>
/// One modal on screen, from <c>PresentAsync</c> until it closes, and the view models' handle on it.
/// <para>
/// Why it exists: a modal is several things that must stay consistent: the page pushed with
/// <c>PushModalAsync</c>, the pages pushed inside it, and the caller awaiting its result. Keeping them in one
/// object (one per presentation, a coordinator) means a page asks <em>its</em> modal to push, pop or close,
/// instead of the services guessing which modal is meant from the window's modal stack.
/// </para>
/// <para>
/// <see cref="ModalService"/> opens it, hands it to every page it creates for the modal, and tells it when MAUI
/// removed the modal (<see cref="OnClosed"/>). The platform work goes through <see cref="IModalPresenter"/>;
/// this class holds the state and the rules.
/// </para>
/// </summary>
internal class ModalPresentation
{
    private IModalPresenter? _presenter;

    // The cancel dismissal in progress, shared by concurrent callers on any page (e.g. Back pressed twice)
    private Task<bool>? _pendingCancel;

    /// <summary>The page pushed with <c>PushModalAsync</c>: the first page, or the NavigationPage hosting it.</summary>
    public Page? Host { get; private set; }

    /// <summary>The page presented first; its view model answers Android Back on the modal's first page.</summary>
    public Page? FirstPage { get; private set; }

    public bool IsClosed { get; private set; }

    internal void Open(IModalPresenter presenter, Page firstPage, Page host)
    {
        _presenter = presenter;
        FirstPage = firstPage;
        Host = host;
    }

    /// <summary>Pushes a page inside this modal; it receives this presentation too.</summary>
    public Task<IResult> PushAsync(string pageName, INavigationParameters? parameters, bool animated)
        => Presenter.PushAsync(this, pageName, parameters, animated);

    /// <summary>Goes back one page inside this modal; <c>NotHandled</c> on the first page.</summary>
    public Task<IResult> PopAsync(bool animated) => Presenter.PopAsync(this, animated);

    /// <summary>
    /// Closes the modal without a result after <paramref name="canDismiss"/> agrees; a waiting caller receives
    /// <c>Cancelled</c>. Calls made while a dismissal is in progress share its outcome.
    /// </summary>
    public Task<bool> CancelAsync(Func<Task<bool>> canDismiss)
    {
        if (_pendingCancel is { IsCompleted: false } pending)
            return pending;

        return _pendingCancel = CancelCoreAsync(canDismiss);
    }

    /// <summary>
    /// Closes the modal and hands <paramref name="result"/> to the caller, when one awaits a
    /// <typeparamref name="TResult"/> (<see cref="ModalPresentation{TResult}"/>). Otherwise it only closes.
    /// </summary>
    public Task<IResult> CompleteAsync<TResult>(TResult result)
        => this is ModalPresentation<TResult> awaited
            ? awaited.CompleteWithResultAsync(result)
            : DismissAsync();

    /// <summary>MAUI removed the modal, by any route. Completes a waiting caller.</summary>
    internal virtual void OnClosed() => IsClosed = true;

    // Closes this presentation once the modal is removed. ModalService has usually closed it already, from MAUI's
    // ModalPopped, before the pop returns; closing is idempotent.
    private protected async Task<IResult> DismissAsync()
    {
        var dismissed = await Presenter.DismissAsync(this);
        if (dismissed.IsSuccess)
            OnClosed();

        return dismissed;
    }

    private async Task<bool> CancelCoreAsync(Func<Task<bool>> canDismiss)
    {
        if (await canDismiss() == false)
            return false;

        return (await DismissAsync()).IsSuccess;
    }

    private IModalPresenter Presenter
        => _presenter ?? throw new InvalidOperationException("The modal hasn't been presented yet.");
}

/// <summary>
/// A presentation whose caller awaits a <typeparamref name="TResult"/>
/// (<c>PresentAsync&lt;TViewModel, TResult&gt;</c>).
/// <para>
/// Why it's separate: <c>PresentAsync(name)</c> presents a modal nobody awaits, which has no result type.
/// The type here is fixed by the caller, and the pages that can complete it are checked against it at compile
/// time (<c>ModalViewModel&lt;TResult&gt;.PushAsync</c>), so a result of another type can't reach it.
/// </para>
/// </summary>
internal sealed class ModalPresentation<TResult> : ModalPresentation
{
    // Asynchronous continuations: OnClosed runs inside MAUI's pop, and the caller may present the next modal
    private readonly TaskCompletionSource<TResult> _caller = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _hasResult;
    private TResult? _result;

    /// <summary>The caller's result: completed when the modal closes, cancelled if it closed without one.</summary>
    public Task<TResult> Result => _caller.Task;

    internal async Task<IResult> CompleteWithResultAsync(TResult result)
    {
        SetPendingResult(result);

        var dismissed = await DismissAsync();
        if (dismissed.IsFailure)
            ClearPendingResult(); // the modal is still open: a later Cancel must not return this result

        return dismissed;
    }

    // Set before the pop because the pop closes the presentation (ModalPopped) before it returns
    internal void SetPendingResult(TResult result)
    {
        _result = result;
        _hasResult = true;
    }

    private void ClearPendingResult()
    {
        _result = default;
        _hasResult = false;
    }

    internal override void OnClosed()
    {
        if (IsClosed)
            return;

        base.OnClosed();

        if (_hasResult)
            _caller.TrySetResult(_result!);
        else
            _caller.TrySetCanceled();
    }
}
