using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class FlyoutViewModel : BaseViewModel, IFlyoutComponent, IDisposable
{
    protected virtual void OnFlyoutOpened() { }

    protected virtual Task OnFlyoutOpenedAsync() => Task.CompletedTask;

    protected virtual void OnFlyoutClosed() { }

    protected virtual Task OnFlyoutClosedAsync() => Task.CompletedTask;

    protected virtual void OnDispose() { }

    void IFlyoutComponent.OnFlyoutOpened() => OnFlyoutOpened();

    void IFlyoutComponent.OnFlyoutClosed() => OnFlyoutClosed();

    Task IFlyoutComponent.OnFlyoutOpenedAsync() => OnFlyoutOpenedAsync();

    Task IFlyoutComponent.OnFlyoutClosedAsync() => OnFlyoutClosedAsync();

#pragma warning disable CA1816
    void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}