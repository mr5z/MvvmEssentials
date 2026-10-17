using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Modals;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IModalViewModel;

public interface IModalViewModel<TResult> : IModalViewModel;

public partial class ModalViewModel<TResult>(IModalService modalService)
	: PageViewModel, IModalViewModel<TResult>, IModalDismissible
{
	private readonly ModalSession<TResult> _session = new(modalService);

	private protected override void HandleParametersSet(INavigationParameters parameters)
	{
		base.HandleParametersSet(parameters);

		_session.Attach(parameters);
	}

	private protected override void HandleDispose()
	{
		base.HandleDispose();

		// Disposed without going through Dismiss(): Android back button, iOS sheet swipe-down
		_session.NotifyDisposed();
	}

	/// <summary>
	/// Asked before the modal is dismissed without a result: the <see cref="Dismiss()"/> command
	/// (Cancel / close) and the Android Back button.
	/// Return <c>false</c> to keep the modal open, for example after the user chose not to discard changes.
	/// <para>
	/// Not called for <see cref="Dismiss(TResult)"/>, and not called for the iOS sheet swipe-down gesture,
	/// which MAUI doesn't let the app intercept.
	/// </para>
	/// </summary>
	protected virtual Task<bool> CanDismissAsync() => Task.FromResult(true);

	/// <summary>
	/// Dismisses without a result (Cancel / close); the caller awaiting the modal receives <c>ErrorCode.Cancelled</c>.
	/// Returns <c>false</c> if <see cref="CanDismissAsync"/> declined or the modal could not be dismissed.
	/// Calls made while a dismissal is in progress share its outcome.
	/// </summary>
	[RelayCommand]
	public Task<bool> Dismiss() => _session.DismissAsync(CanDismissAsync);

	protected Task<IResult> Dismiss(TResult result) => _session.DismissAsync(result);
}