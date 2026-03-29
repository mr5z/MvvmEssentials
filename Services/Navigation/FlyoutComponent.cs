namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IFlyoutComponent
{
    void OnFlyoutOpened();

    void OnFlyoutClosed();

    Task OnFlyoutOpenedAsync();

    Task OnFlyoutClosedAsync();
}