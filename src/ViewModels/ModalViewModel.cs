using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IModalViewModel;

public interface IModalViewModel<TResult> : IModalViewModel;

public partial class ModalViewModel<TResult>(IModalService modalService)
	: PageViewModel, IModalViewModel<TResult>, IModalDismissible
{
	private readonly IModalService _modalService = modalService;

	private TaskCompletionSource<TResult>? _completion;
	
	// Set while Dismiss() is popping this modal, so the resulting unload isn't mistaken for a system dismissal
	private bool _isDismissing;

	// The cancel dismissal in progress, shared by concurrent callers (e.g. Back pressed twice during a prompt)
	private Task<bool>? _pendingDismiss;

	private protected override void HandleParametersSet(INavigationParameters parameters)
	{
		base.HandleParametersSet(parameters);

		if (parameters.TryGetValue<TaskCompletionSource<TResult>>(NavigationHints.PopupCompletionParam, out var completion))
		{
			_completion = completion;
		}
	}

	private protected override void HandleDispose()
	{
		base.HandleDispose();

		// Disposed without going through Dismiss(): Android back button, iOS sheet swipe-down
		if (_isDismissing == false)
		{
			_completion?.TrySetCanceled();
		}
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
	public Task<bool> Dismiss()
	{
		if (_pendingDismiss is { IsCompleted: false } pending)
			return pending;

		return _pendingDismiss = DismissWithoutResultAsync();
	}

	protected async Task<IResult> Dismiss(TResult result)
	{
		var modalResult = await DismissModalAsync();
		if (modalResult.IsSuccess)
		{
			_completion?.TrySetResult(result);
		}
		else
		{
			_completion?.TrySetException(new Exception(modalResult.ErrorMessage));
		}

		return modalResult;
	}

	private async Task<bool> DismissWithoutResultAsync()
	{
		if (await CanDismissAsync() == false)
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