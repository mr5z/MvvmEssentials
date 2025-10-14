namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IPopupDismissible
{
	Task<bool> Dismiss();

	void NotifyCancellation();

	bool ShouldHandleBackButtonPressed { get; }

	bool ShouldDismissOnBackgroundClicked { get; }
}
