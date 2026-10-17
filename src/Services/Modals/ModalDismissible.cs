namespace Nkraft.MvvmEssentials.Services.Modals;

/// <summary>
/// Lets <see cref="ModalService"/> route the Android Back button to a modal's view model.
/// </summary>
internal interface IModalDismissible
{
    /// <summary>
    /// Handles Back: dismisses the modal without a result after the view model's dismissal guard agrees,
    /// or, for a modal wizard, goes to the previous step instead. Returns <c>true</c> only if the modal was dismissed.
    /// </summary>
    Task<bool> Dismiss();
}
