using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Popups;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IPopupViewModel;

public interface IPopupViewModel<TResult> : IPopupViewModel;

/// <summary>
/// A popup that returns a <typeparamref name="TResult"/> to the caller of
/// <c>IPopupService.PresentAsync&lt;TViewModel, TResult&gt;</c>.
/// <para>
/// A background tap or Android Back is routed to <c>Dismiss()</c> when the matching <c>ShouldDismissOn...</c> allows
/// it, so <c>CanDismissAsync</c> can keep the popup open.
/// </para>
/// </summary>
public class PopupViewModel<TResult>(IPopupService popupService)
	: PresentedViewModel<TResult>, IPopupViewModel<TResult>, IPopupDismissible
{
	private readonly IPopupService _popupService = popupService;

	protected virtual bool ShouldDismissOnBackButtonPressed => true;

	protected virtual bool ShouldDismissOnBackgroundTapped => true;

	bool IPopupDismissible.ShouldDismissOnBackButtonPressed => ShouldDismissOnBackButtonPressed;

	bool IPopupDismissible.ShouldDismissOnBackgroundTapped => ShouldDismissOnBackgroundTapped;

	private protected override Task<IResult> DismissTopAsync() => _popupService.DismissAsync();

	internal sealed override string PageName => PageHelper.ToPageName(GetType(), PagePattern.Popup);
}
