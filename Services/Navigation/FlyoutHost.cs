namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IFlyoutHost
{
    bool IsPresented { get; set; }

    IFlyoutComponent MenuViewModel { get; }

    IFlyoutComponent DetailViewModel { get; }
}