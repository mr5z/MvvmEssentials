using Nkraft.MvvmEssentials.Services.FlyoutPages;

namespace Nkraft.MvvmEssentials.ViewModels;

internal interface IInitialDetail
{
    Page? DetailPage { get; set; } 
}

public abstract class FlyoutViewModel<TMenu, TDetail>(TMenu menu, TDetail detail) : PageViewModel, IFlyoutHost, IInitialDetail
    where TMenu : FlyoutMenuViewModel
    where TDetail : IFlyoutComponent
{
    public TMenu MenuViewModel { get; } = menu;
    public TDetail DetailViewModel { get; } = detail;
    public bool IsPresented { get; set; }
    
    IFlyoutComponent IFlyoutHost.MenuViewModel => MenuViewModel;
    IFlyoutComponent IFlyoutHost.DetailViewModel => DetailViewModel;
    
    Page? IInitialDetail.DetailPage { get; set; }
}
