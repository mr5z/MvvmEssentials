using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class FlyoutHostViewModel(IFlyoutComponent menu, IFlyoutComponent detail) : PageViewModel, IFlyoutHost
{
    public IFlyoutComponent MenuViewModel { get; } = menu;

    public IFlyoutComponent DetailViewModel { get; } = detail;

    public bool IsPresented { get; set; }
}
