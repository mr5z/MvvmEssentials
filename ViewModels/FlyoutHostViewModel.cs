using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class FlyoutHostViewModel : PageViewModel, IFlyoutHost
{
    public abstract PageViewModel FlyoutViewModel { get; }
    public abstract PageViewModel DetailViewModel { get; }

    bool IFlyoutHost.IsPresented { get; set; }
}
