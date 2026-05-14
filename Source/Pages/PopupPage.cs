using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Pages;

public class PopupPage : Mopups.Pages.PopupPage
{
	protected override bool OnBackgroundClicked()
	{
		if (BindingContext is IPopupDismissible { ShouldDismissOnBackgroundTapped: true } dismissible)
			// The word use should be "tap", not "click" here, since it's mobile.
		{
			dismissible.NotifyCancellation();
		}

		return true;
	}

	protected override bool OnBackButtonPressed()
	{
		if (BindingContext is IPopupDismissible { ShouldDismissOnBackButtonPressed: true } dismissible)
		{
			dismissible.NotifyCancellation();
		}

		return true;
	}
}
