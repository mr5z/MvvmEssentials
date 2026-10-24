using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Presentations;

/// <summary>
/// One presented page (a popup or a modal) from <c>PresentAsync</c> until it is removed, and its view models'
/// handle on it.
/// <para>
/// Why it exists: a presented page is several things that must stay consistent: the page on screen, the pages pushed
/// inside it (modals), and the caller awaiting its result. One object per presentation keeps them together, so a
/// view model asks <em>its</em> presentation to dismiss or navigate instead of a service guessing which page is
/// meant. The same object serves popups, modals and a modally presented wizard, which can't share a base class.
/// </para>
/// <para>
/// The presenting service opens it, hands it to every page it creates for the presentation, and reports the removal
/// (<see cref="OnClosed"/>). The platform work goes through <see cref="IPresenter"/>; this class holds the state and
/// the rules.
/// </para>
/// </summary>
internal class Presentation
{
    private IPresenter? _presenter;

    // The cancel dismissal in progress, shared by concurrent callers on any page (e.g. Back pressed twice)
    private Task<bool>? _pendingCancel;

    /// <summary>The page the presenter put on screen: the popup, or the modal's first page or the NavigationPage hosting it.</summary>
    public Page? Host { get; private set; }

    /// <summary>The page presented first; its view model answers Back on a modal's first page.</summary>
    public Page? FirstPage { get; private set; }

    public bool IsClosed { get; private set; }

    internal void Open(IPresenter presenter, Page firstPage, Page host)
    {
        _presenter = presenter;
        FirstPage = firstPage;
        Host = host;
    }

    /// <summary>
    /// Pushes a page inside this presentation; it receives this presentation too. Only presenters with a page stack
    /// (modals) support it.
    /// </summary>
    public Task<IResult> PushAsync(string pageName, INavigationParameters? parameters, bool animated)
        => Presenter is IStackPresenter stack
            ? stack.PushAsync(this, pageName, parameters, animated)
            : Task.FromResult(NoStack());

    /// <summary>Goes back one page inside this presentation; <c>NotHandled</c> on the first page.</summary>
    public Task<IResult> PopAsync(bool animated)
        => Presenter is IStackPresenter stack
            ? stack.PopAsync(this, animated)
            : Task.FromResult(NoStack());

    /// <summary>
    /// Pushes a page inside this presentation, then removes the page it covers, so going back skips it. Only
    /// presenters with a page stack (modals) support it.
    /// </summary>
    public Task<IResult> ReplaceAsync(string pageName, INavigationParameters? parameters, bool animated)
        => Presenter is IStackPresenter stack
            ? stack.ReplaceAsync(this, pageName, parameters, animated)
            : Task.FromResult(NoStack());

    /// <summary>The first page was replaced (<see cref="ReplaceAsync"/>): Back on the new one asks its view model.</summary>
    internal void ReplaceFirstPage(Page page) => FirstPage = page;

    /// <summary>
    /// Removes the page without a result after <paramref name="canDismiss"/> agrees; a waiting caller receives
    /// <c>Cancelled</c>. Calls made while a dismissal is in progress share its outcome.
    /// </summary>
    public Task<bool> CancelAsync(Func<Task<bool>> canDismiss)
    {
        if (_pendingCancel is { IsCompleted: false } pending)
            return pending;

        return _pendingCancel = CancelCoreAsync(canDismiss);
    }

    /// <summary>
    /// Removes the page and hands <paramref name="result"/> to the caller, when one awaits a
    /// <typeparamref name="TResult"/> (<see cref="Presentation{TResult}"/>). Otherwise it only removes the page.
    /// </summary>
    public Task<IResult> CompleteAsync<TResult>(TResult result)
        => this is Presentation<TResult> awaited
            ? awaited.CompleteWithResultAsync(result)
            : DismissAsync();

    /// <summary>The page was removed, by any route. Completes a waiting caller.</summary>
    internal virtual void OnClosed() => IsClosed = true;

    // Closes this presentation once the page is removed. The presenter has usually closed it already, from the
    // platform's removal event, before the removal returns; closing is idempotent.
    private protected async Task<IResult> DismissAsync()
    {
        var dismissed = await Presenter.DismissAsync(this);
        if (dismissed.IsSuccess)
        {
            OnClosed();
        }

        return dismissed;
    }

    private async Task<bool> CancelCoreAsync(Func<Task<bool>> canDismiss)
    {
        if (await canDismiss() == false)
            return false;

        return (await DismissAsync()).IsSuccess;
    }

    private IPresenter Presenter
        => _presenter ?? throw new InvalidOperationException("The page hasn't been presented yet.");

    private static IResult NoStack()
        => Result.Fail(ErrorCode.NotSupported, "This presentation has no page stack to navigate; only modals do.");
}

/// <summary>
/// A presentation whose caller awaits a <typeparamref name="TResult"/>
/// (<c>PresentAsync&lt;TViewModel, TResult&gt;</c>).
/// <para>
/// Why it's separate: <c>PresentAsync(name)</c> presents a page nobody awaits, which has no result type. The type
/// here is fixed by the caller, and the pages that can complete it are checked against it at compile time, so a
/// result of another type can't reach it.
/// </para>
/// </summary>
internal sealed class Presentation<TResult> : Presentation
{
    // Asynchronous continuations: OnClosed runs inside the platform's removal, and the caller may present the next page
    private readonly TaskCompletionSource<TResult> _caller = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _hasResult;
    private TResult? _result;

    /// <summary>The caller's result: completed when the page is removed, cancelled if it was removed without one.</summary>
    public Task<TResult> Result => _caller.Task;

    internal async Task<IResult> CompleteWithResultAsync(TResult result)
    {
        SetPendingResult(result);

        var dismissed = await DismissAsync();
        if (dismissed.IsFailure)
        {
            // The page is still on screen: a later Cancel must not return this result
            ClearPendingResult();
        }

        return dismissed;
    }

    // Set before the removal because the removal closes the presentation before it returns
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
        {
            _caller.TrySetResult(_result!);
        }
        else
        {
            _caller.TrySetCanceled();
        }
    }
}
