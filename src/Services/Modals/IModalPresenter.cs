using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Modals;

/// <summary>
/// The platform work a <see cref="ModalPresentation"/> asks for: pushing and popping pages inside its modal,
/// and removing the modal. Implemented by <see cref="ModalService"/>.
/// <para>
/// Why it's an interface: the presentation holds the modal's state and rules (who can complete it, what Cancel
/// returns); the service owns the window and the page factory. Keeping the presentation on this interface lets its
/// rules be tested without a window, and keeps <see cref="IModalService"/> free of per-modal operations.
/// </para>
/// </summary>
internal interface IModalPresenter
{
    Task<IResult> PushAsync(ModalPresentation presentation, string pageName, INavigationParameters? parameters, bool animated);

    Task<IResult> PopAsync(ModalPresentation presentation, bool animated);

    /// <summary>Removes the presentation's modal from the window.</summary>
    Task<IResult> DismissAsync(ModalPresentation presentation);
}
