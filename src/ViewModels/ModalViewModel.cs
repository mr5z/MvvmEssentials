using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IModalViewModel;

public interface IModalViewModel<TResult> : IModalViewModel;

/// <summary>
/// A page of a modal that returns a <typeparamref name="TResult"/> to the caller of
/// <c>PresentAsync&lt;TViewModel, TResult&gt;</c>.
/// <para>
/// A modal can hold several of these pages: <see cref="PushAsync(ModalDestination{TResult}, bool)"/> pushes the next
/// one inside the modal, and only accepts pages of the same <typeparamref name="TResult"/>, so any page of the modal can
/// finish it with <see cref="Dismiss(TResult)"/>. Pass data to the next page through its navigation parameters
/// (<c>With(...)</c>).
/// </para>
/// </summary>
public partial class ModalViewModel<TResult>(IModalService modalService)
	: PageViewModel, IModalViewModel<TResult>, IModalDismissible
{
	private readonly IModalService _modalService = modalService;

	// The modal this page belongs to; set when ModalService created the page
	private ModalPresentation? _presentation;

	private protected override void HandleParametersSet(INavigationParameters parameters)
	{
		base.HandleParametersSet(parameters);

		parameters.TryGetValue(NavigationHints.ModalPresentation, out _presentation);
	}

	/// <summary>
	/// Asked before the modal is dismissed without a result: the <see cref="Dismiss()"/> command
	/// (Cancel / close) and the Android Back button on the modal's first page.
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
	/// Calls made while a dismissal is in progress, from any page of the modal, share its outcome.
	/// </summary>
	[RelayCommand]
	public Task<bool> Dismiss()
		=> _presentation?.CancelAsync(CanDismissAsync) ?? DismissTopModalAsync();

	/// <summary>
	/// Dismisses the modal and hands <paramref name="result"/> to the caller.
	/// </summary>
	protected Task<IResult> Dismiss(TResult result)
		=> _presentation?.CompleteAsync(result) ?? _modalService.DismissAsync();

	/// <summary>
	/// Pushes the next page inside this modal, e.g. <c>PushAsync(ConfirmViewModel.With(draft))</c>. The destination's
	/// result type must be this modal's, so the pushed page can finish it too.
	/// Fails with <c>ErrorCode.NotSupported</c> when the modal sets <c>Modal.WithNavigation="False"</c>.
	/// </summary>
	protected Task<IResult> PushAsync(ModalDestination<TResult> destination, bool animated = true)
		=> InModal(presentation => presentation.PushAsync(destination.ModalName, destination.Parameters, animated));

	/// <summary>
	/// Pushes <typeparamref name="TNext"/> inside this modal, for a page without navigation parameters.
	/// </summary>
	protected Task<IResult> PushAsync<TNext>(bool animated = true) where TNext : IModalViewModel<TResult>
		=> InModal(presentation => presentation.PushAsync(PageHelper.ToPageName<TNext>(PagePattern.Page), null, animated));

	/// <summary>
	/// Goes back to the previous page inside this modal. Returns <c>ErrorCode.NotHandled</c> on the modal's first
	/// page: leave the modal with <see cref="Dismiss()"/> there.
	/// </summary>
	protected Task<IResult> PopAsync(bool animated = true)
		=> InModal(presentation => presentation.PopAsync(animated));

	// Shown other than by IModalService (e.g. a direct PushModalAsync): close whatever modal is on top
	private async Task<bool> DismissTopModalAsync()
	{
		if (await CanDismissAsync() == false)
			return false;

		return (await _modalService.DismissAsync()).IsSuccess;
	}

	private Task<IResult> InModal(Func<ModalPresentation, Task<IResult>> navigate)
	{
		if (_presentation is null)
		{
			const string message = "This page wasn't presented with IModalService, so it can't navigate inside a modal.";
			return Task.FromResult<IResult>(Result.Fail(ErrorCode.InvalidState, message));
		}

		return navigate(_presentation);
	}
}
