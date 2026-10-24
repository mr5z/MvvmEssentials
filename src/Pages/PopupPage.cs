using Nkraft.CrossUtility.Extensions;
using Nkraft.MvvmEssentials.Services.Popups;

namespace Nkraft.MvvmEssentials.Pages;

public class PopupPage : Nkraft.Mopups.Pages.PopupPage
{
	// "Tap", not "click": it's mobile.
	protected sealed override bool OnBackgroundClicked()
	{
		if (BindingContext is IPopupDismissible { ShouldDismissOnBackgroundTapped: true } dismissible)
		{
			DismissThroughViewModel(dismissible);
		}

		return true;
	}

	protected sealed override bool OnBackButtonPressed()
	{
		if (BindingContext is IPopupDismissible { ShouldDismissOnBackButtonPressed: true } dismissible)
		{
			DismissThroughViewModel(dismissible);
		}

		return true;
	}

	// The view model's CanDismissAsync decides; the popup service removes the page and completes the caller
	private static void DismissThroughViewModel(IPopupDismissible dismissible)
		=> dismissible.Dismiss().FireAndForget(ex => System.Diagnostics.Trace.TraceError(
			"Dismissing popup after a background tap or Back failed: {0}", ex));
}
