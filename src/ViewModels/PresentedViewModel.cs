using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Presentations;

namespace Nkraft.MvvmEssentials.ViewModels;

/// <summary>
/// A page presented by a service (<see cref="PopupViewModel{TResult}"/>, <see cref="ModalViewModel{TResult}"/>)
/// that returns a <typeparamref name="TResult"/> to the caller of <c>PresentAsync&lt;TViewModel, TResult&gt;</c>.
/// <para>
/// Dismissing is the same for both: <see cref="Dismiss()"/> closes with <c>Cancelled</c> after
/// <see cref="CanDismissAsync"/> agrees, <see cref="Dismiss(TResult)"/> closes with a result. What differs (where
/// the page lives, how the platform removes it) is the service's business.
/// </para>
/// </summary>
public abstract partial class PresentedViewModel<TResult> : PageViewModel, IDismissible
{
	// The presentation this page belongs to; set when a presenting service created the page
	private Presentation? _presentation;

	private protected override void HandleParametersSet(INavigationParameters parameters)
	{
		base.HandleParametersSet(parameters);

		parameters.TryGetValue(NavigationHints.Presentation, out _presentation);
	}

	private protected bool IsPresented => _presentation is not null;

	/// <summary>
	/// Asked before the page is dismissed without a result: the <see cref="Dismiss()"/> command (Cancel / close)
	/// and platform dismissals routed to it (Android Back; a popup's background tap).
	/// Return <c>false</c> to keep the page open, for example after the user chose not to discard changes.
	/// Not called for <see cref="Dismiss(TResult)"/>.
	/// </summary>
	protected virtual Task<bool> CanDismissAsync() => Task.FromResult(true);

	/// <summary>
	/// Dismisses without a result (Cancel / close); the caller awaiting the page receives <c>ErrorCode.Cancelled</c>.
	/// Returns <c>false</c> if <see cref="CanDismissAsync"/> declined or the page could not be dismissed.
	/// Calls made while a dismissal is in progress share its outcome.
	/// </summary>
	[RelayCommand]
	public Task<bool> Dismiss()
		=> _presentation?.CancelAsync(CanDismissAsync) ?? DismissUnpresentedAsync();

	/// <summary>
	/// Dismisses the page and hands <paramref name="result"/> to the caller.
	/// </summary>
	protected Task<IResult> Dismiss(TResult result)
		=> _presentation?.CompleteAsync(result) ?? DismissTopAsync();

	/// <summary>
	/// Removes whatever the service shows on top, for a page shown other than through the service
	/// (e.g. a direct <c>PushModalAsync</c>), which has no presentation of its own.
	/// </summary>
	private protected abstract Task<IResult> DismissTopAsync();

	private protected Task<IResult> InPresentation(Func<Presentation, Task<IResult>> navigate)
	{
		if (_presentation is null)
		{
			const string message = "This page wasn't presented by its service, so it can't navigate inside a presentation.";
			return Task.FromResult<IResult>(Result.Fail(ErrorCode.InvalidState, message));
		}

		return navigate(_presentation);
	}

	private async Task<bool> DismissUnpresentedAsync()
	{
		if (await CanDismissAsync() == false)
			return false;

		return (await DismissTopAsync()).IsSuccess;
	}
}
