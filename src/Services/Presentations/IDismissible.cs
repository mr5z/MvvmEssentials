namespace Nkraft.MvvmEssentials.Services.Presentations;

/// <summary>
/// Lets a presented page route a platform dismissal (Android Back, a popup's background tap) to its view model,
/// so the view model's dismissal guard applies.
/// </summary>
internal interface IDismissible
{
    /// <summary>
    /// Dismisses without a result after the view model's guard agrees, or, for a modal wizard, goes to the previous
    /// step instead. Returns <c>true</c> only if the page was dismissed.
    /// </summary>
    Task<bool> Dismiss();
}
