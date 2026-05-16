using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class FlyoutHostViewModel<TMenu, TDetail>(TMenu menu, TDetail detail) : PageViewModel, IFlyoutHost
    where TMenu : FlyoutMenuViewModel
    where TDetail : IFlyoutComponent
{
    IFlyoutComponent IFlyoutHost.MenuViewModel => menu;
    IFlyoutComponent IFlyoutHost.DetailViewModel => detail;
    public bool IsPresented { get; set; }
}
