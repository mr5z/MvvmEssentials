using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class FlyoutMenuViewModel : BaseViewModel, IFlyoutComponent, IDisposable
{
    private IFlyoutHost? _flyoutHost;
    
    internal void SetFlyoutHost(IFlyoutHost flyoutHost)
    {
        _flyoutHost = flyoutHost;
    }
    
    protected bool IsPresented
    {
        get => _flyoutHost?.IsPresented ?? false;
        set => _flyoutHost?.IsPresented = value;
    }
    
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