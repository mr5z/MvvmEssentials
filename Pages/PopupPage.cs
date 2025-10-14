using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Pages;

public class PopupPage : Mopups.Pages.PopupPage
{
	protected override bool OnBackgroundClicked()
	{
		var dismissible = BindingContext as IPopupDismissible;
		if (dismissible is not null)
		{
			var dismiss = dismissible.ShouldDismissOnBackgroundClicked;
			if (dismiss == false)
			{
				return false;
			}
		}

		var willDismiss = base.OnBackgroundClicked();
		if (willDismiss)
		{
			dismissible?.NotifyCancellation();
		}

		return willDismiss;
	}

	// TODO this is actually confusing
	// review again in the future
	protected override bool OnBackButtonPressed()
	{
		var dismissible = BindingContext as IPopupDismissible;
		if (dismissible is not null)
		{
			var dismiss  = dismissible.ShouldHandleBackButtonPressed;
			if (dismiss == false)
			{
				dismissible?.NotifyCancellation();
				return true;
			}
		}
		
		var backPressedHandled = base.OnBackButtonPressed();
		if (backPressedHandled == false)
		{
			dismissible?.NotifyCancellation();
		}

		return backPressedHandled;
	}
}
