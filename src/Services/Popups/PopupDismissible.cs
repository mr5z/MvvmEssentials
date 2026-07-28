namespace Nkraft.MvvmEssentials.Services.Popups;

internal interface IPopupDismissible
{
	Task<bool> Dismiss();

	void NotifyCancellation();

	bool ShouldDismissOnBackButtonPressed { get; }

	bool ShouldDismissOnBackgroundTapped { get; }
}
