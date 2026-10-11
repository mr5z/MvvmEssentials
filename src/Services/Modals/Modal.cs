namespace Nkraft.MvvmEssentials.Services.Modals;

/// <summary>
/// Attached properties for pages presented with <see cref="Services.IModalService"/>.
/// </summary>
public static class Modal
{
    /// <summary>
    /// When <c>true</c> (default), the page is hosted in a <see cref="NavigationPage"/> so it gets the same
    /// top bar as regular pages, including the area behind the status bar.
    /// Set to <c>false</c> for modals that draw their own background, such as overlays or custom sheets.
    /// </summary>
    public static readonly BindableProperty WithNavigationProperty = BindableProperty.CreateAttached(
        "WithNavigation",
        typeof(bool),
        typeof(Modal),
        true);

    public static bool GetWithNavigation(BindableObject view)
        => (bool)view.GetValue(WithNavigationProperty);

    public static void SetWithNavigation(BindableObject view, bool value)
        => view.SetValue(WithNavigationProperty, value);
}