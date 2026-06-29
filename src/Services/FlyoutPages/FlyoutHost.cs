namespace Nkraft.MvvmEssentials.Services.FlyoutPages;

internal interface IFlyoutHost
{
    bool IsPresented { get; set; }

    IFlyoutComponent MenuViewModel { get; }

    IFlyoutComponent DetailViewModel { get; }
}