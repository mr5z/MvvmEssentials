namespace Nkraft.MvvmEssentials.Services.Modals;

/// <summary>
/// Lets <see cref="ModalService"/> route the Android Back button to a modal's view model.
/// </summary>
internal interface IModalDismissible
{
    /// <summary>
    /// Dismisses the modal without a result, after the view model's dismissal guard agrees.
    /// </summary>
    Task<bool> Dismiss();
}