namespace Nkraft.MvvmEssentials.Services.Navigation;

internal static class NavigationHelper
{
    internal static NavigationPage? FindNavigationPage(Page page)
    {
        return page switch
        {
            NavigationPage navPage => navPage,
            FlyoutPage flyoutPage => FindNavigationPage(flyoutPage.Detail),
            TabbedPage { CurrentPage: not null } tabbedPage => FindNavigationPage(tabbedPage.CurrentPage),
            _ => null
        };
    }
}