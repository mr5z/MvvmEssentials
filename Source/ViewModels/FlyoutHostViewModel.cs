using Nkraft.MvvmEssentials.Services.FlyoutPages;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class FlyoutHostViewModel<TMenu, TDetail>(TMenu menu, TDetail detail) : PageViewModel, IFlyoutHost
    where TMenu : FlyoutMenuViewModel
    where TDetail : IFlyoutComponent
{
    public TMenu MenuViewModel { get; } = menu;
    public TDetail DetailViewModel { get; } = detail;
    public bool IsPresented { get; set; }
    
    IFlyoutComponent IFlyoutHost.MenuViewModel => MenuViewModel;
    IFlyoutComponent IFlyoutHost.DetailViewModel => DetailViewModel;
}