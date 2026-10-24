using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Presentations;

/// <summary>
/// The platform work a <see cref="Presentation"/> asks for: removing its page. Implemented by the presenting
/// services (<see cref="PopupService"/>, <see cref="ModalService"/>).
/// <para>
/// Why it's an interface: the presentation holds the state and the rules (who completes it, what Cancel returns);
/// the service owns the window or popup stack and the page factory. The split lets the rules be tested without a
/// window, and keeps the public service interfaces free of per-presentation operations.
/// </para>
/// </summary>
internal interface IPresenter
{
    Task<IResult> DismissAsync(Presentation presentation);
}

/// <summary>
/// A presenter whose presentations have a page stack, so pages can be pushed and popped inside them (modals).
/// </summary>
internal interface IStackPresenter : IPresenter
{
    Task<IResult> PushAsync(Presentation presentation, string pageName, INavigationParameters? parameters, bool animated);

    Task<IResult> PopAsync(Presentation presentation, bool animated);

    Task<IResult> ReplaceAsync(Presentation presentation, string pageName, INavigationParameters? parameters, bool animated);
}
