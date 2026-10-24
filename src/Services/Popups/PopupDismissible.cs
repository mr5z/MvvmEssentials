using Nkraft.MvvmEssentials.Services.Presentations;

namespace Nkraft.MvvmEssentials.Services.Popups;

/// <summary>
/// What <see cref="Pages.PopupPage"/> asks its view model: whether a background tap or Back dismisses, and then to dismiss.
/// </summary>
internal interface IPopupDismissible : IDismissible
{
	bool ShouldDismissOnBackButtonPressed { get; }

	bool ShouldDismissOnBackgroundTapped { get; }
}
