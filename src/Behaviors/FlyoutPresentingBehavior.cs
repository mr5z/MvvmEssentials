using System.ComponentModel;
using Nkraft.CrossUtility.Extensions;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.FlyoutPages;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Behaviors;

public sealed class FlyoutPresentingBehavior : Behavior<FlyoutPage>
{
    private FlyoutPage? _flyoutPage;

    protected override void OnAttachedTo(FlyoutPage bindable)
    {
        base.OnAttachedTo(bindable);
        _flyoutPage = bindable;
        bindable.BindingContextChanged += FlyoutPage_BindingContextChanged;
        bindable.IsPresentedChanged += FlyoutPage_IsPresentedChanged;
        
        SetupFlyoutHost(bindable);
    }

    protected override void OnDetachingFrom(FlyoutPage bindable)
    {
        base.OnDetachingFrom(bindable);
        _flyoutPage = null;
        bindable.BindingContextChanged -= FlyoutPage_BindingContextChanged;
        bindable.IsPresentedChanged -= FlyoutPage_IsPresentedChanged;
        if (bindable.BindingContext is INotifyPropertyChanged notifiable)
        {
            notifiable.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }
    
    private static void SetupFlyoutHost(FlyoutPage flyoutPage)
    {
        if (flyoutPage.BindingContext is not IFlyoutHost vm)
            return;
            
        if (vm.MenuViewModel is FlyoutMenuViewModel menu)
        {
            menu.SetFlyoutHost(vm);
        }
    }

    private static void FlyoutPage_IsPresentedChanged(object? sender, EventArgs e)
    {
        if (sender is not FlyoutPage flyoutPage)
            return;

        if (flyoutPage.BindingContext is not IFlyoutHost vm)
            return;

        vm.IsPresented = flyoutPage.IsPresented;

        IFlyoutComponent[] components = [ vm.MenuViewModel, vm.DetailViewModel ];

        if (flyoutPage.IsPresented)
        {
            foreach (var component in components)
            {
                component.OnFlyoutOpened();
                component.OnFlyoutOpenedAsync().FireAndForget(ex =>
                {
                    ExceptionDispatcher.Handle<FlyoutPresentingBehavior>(ex, nameof(IFlyoutComponent.OnFlyoutOpenedAsync));
                });
            }
        }
        else
        {
            foreach (var component in components)
            {
                component.OnFlyoutClosed();
                component.OnFlyoutClosedAsync().FireAndForget(ex =>
                {
                    ExceptionDispatcher.Handle<FlyoutPresentingBehavior>(ex, nameof(IFlyoutComponent.OnFlyoutClosedAsync));
                });
            }
        }
    }

    private void FlyoutPage_BindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable)
            return;
        
        if (sender is FlyoutPage flyoutPage)
        {
            SetupFlyoutHost(flyoutPage);
        }

        if (bindable.BindingContext is INotifyPropertyChanged vm)
        {
            vm.PropertyChanged -= ViewModel_PropertyChanged;
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IFlyoutHost.IsPresented))
            return;

        if (_flyoutPage?.BindingContext is IFlyoutHost vm)
        {
            _flyoutPage.IsPresented = vm.IsPresented;
        }
    }
}