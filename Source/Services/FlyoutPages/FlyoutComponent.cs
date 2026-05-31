namespace Nkraft.MvvmEssentials.Services.FlyoutPages;

public interface IFlyoutComponent
{
    void OnFlyoutOpened();

    void OnFlyoutClosed();

    Task OnFlyoutOpenedAsync();

    Task OnFlyoutClosedAsync();
}