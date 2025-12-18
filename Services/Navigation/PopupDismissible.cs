namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IPopupDismissible
{
	Task<bool> Dismiss();

	void NotifyCancellation();

	bool ShouldDismissOnBackButtonPressed { get; }

	bool ShouldDismissOnBackgroundTapped { get; }
}
