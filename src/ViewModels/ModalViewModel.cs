using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IModalViewModel;

public interface IModalViewModel<TResult> : IModalViewModel;

/// <summary>
/// A page of a modal that returns a <typeparamref name="TResult"/> to the caller of
/// <c>IModalService.PresentAsync&lt;TViewModel, TResult&gt;</c>.
/// <para>
/// A modal can hold several of these pages: <see cref="PushAsync(ModalDestination{TResult}, bool)"/> pushes the next
/// one inside the modal, and only accepts pages of the same <typeparamref name="TResult"/>, so any page of the modal can
/// finish it with <c>Dismiss(result)</c>. Pass data to the next page through its navigation parameters (<c>With(...)</c>).
/// <see cref="ReplaceAsync(ModalDestination{TResult}, bool)"/> does the same but removes the page it covers, for a page
/// that mustn't be gone back to.
/// </para>
/// <para>
/// Android Back goes to the previous page inside the modal, and on the first page is routed to <c>Dismiss()</c>, so
/// <c>CanDismissAsync</c> can keep the modal open. The iOS sheet swipe-down can't be intercepted.
/// </para>
/// </summary>
public class ModalViewModel<TResult>(IModalService modalService)
	: PresentedViewModel<TResult>, IModalViewModel<TResult>
{
	private readonly IModalService _modalService = modalService;

	/// <summary>
	/// Pushes the next page inside this modal, e.g. <c>PushAsync(ConfirmViewModel.With(draft))</c>. The destination's
	/// result type must be this modal's, so the pushed page can finish it too.
	/// Fails with <c>ErrorCode.NotSupported</c> when the modal sets <c>Modal.WithNavigation="False"</c>.
	/// </summary>
	protected Task<IResult> PushAsync(ModalDestination<TResult> destination, bool animated = true)
		=> InPresentation(presentation => presentation.PushAsync(destination.ModalName, destination.Parameters, animated));

	/// <summary>
	/// Pushes <typeparamref name="TNext"/> inside this modal, for a page without navigation parameters.
	/// </summary>
	protected Task<IResult> PushAsync<TNext>(bool animated = true) where TNext : IModalViewModel<TResult>
		=> InPresentation(presentation => presentation.PushAsync(PageHelper.ToPageName<TNext>(PagePattern.Page), null, animated));

	/// <summary>
	/// Replaces the modal's current page with the next one: pushes it, then removes the page it covers, so Back and
	/// <see cref="PopAsync"/> skip that page. Use it when going back would make no sense, e.g. from a task's outcome to
	/// the task it ended. When the replaced page was the modal's first, the new page becomes the first: Android Back
	/// there goes to its <c>Dismiss()</c>, so its <c>CanDismissAsync</c> decides.
	/// Fails with <c>ErrorCode.NotSupported</c> when the modal sets <c>Modal.WithNavigation="False"</c>.
	/// </summary>
	protected Task<IResult> ReplaceAsync(ModalDestination<TResult> destination, bool animated = true)
		=> InPresentation(presentation => presentation.ReplaceAsync(destination.ModalName, destination.Parameters, animated));

	/// <summary>
	/// Replaces the modal's current page with <typeparamref name="TNext"/>, for a page without navigation parameters.
	/// </summary>
	protected Task<IResult> ReplaceAsync<TNext>(bool animated = true) where TNext : IModalViewModel<TResult>
		=> InPresentation(presentation => presentation.ReplaceAsync(PageHelper.ToPageName<TNext>(PagePattern.Page), null, animated));

	/// <summary>
	/// Goes back to the previous page inside this modal. Returns <c>ErrorCode.NotHandled</c> on the modal's first
	/// page: leave the modal with <c>Dismiss()</c> there.
	/// </summary>
	protected Task<IResult> PopAsync(bool animated = true)
		=> InPresentation(presentation => presentation.PopAsync(animated));

	private protected override Task<IResult> DismissTopAsync() => _modalService.DismissAsync();
}
