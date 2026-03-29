using System.ComponentModel;
using Nkraft.CrossUtility.Extensions;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Behaviors;

public sealed class FlyoutBehavior : Behavior<FlyoutPage>
{
    private FlyoutPage? _flyoutPage;

    protected override void OnAttachedTo(FlyoutPage bindable)
    {
        base.OnAttachedTo(bindable);
        _flyoutPage = bindable;
        bindable.BindingContextChanged += FlyoutPage_BindingContextChanged;
        bindable.IsPresentedChanged += FlyoutPage_IsPresentedChanged;
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

    private static void FlyoutPage_IsPresentedChanged(object? sender, EventArgs e)
    {
        if (sender is not FlyoutPage flyoutPage)
            return;

        if (flyoutPage.BindingContext is not IFlyoutHost flyoutHost)
            return;

        flyoutHost.IsPresented = flyoutPage.IsPresented;

        IFlyoutComponent[] components = [ flyoutHost.MenuViewModel, flyoutHost.DetailViewModel ];

        if (flyoutPage.IsPresented)
        {
            foreach (var component in components)
            {
                component.OnFlyoutOpened();
                component.OnFlyoutOpenedAsync();
            }
        }
        else
        {
            foreach (var component in components)
            {
                component.OnFlyoutClosed();
                component.OnFlyoutClosedAsync();
            }
        }
    }

    private void FlyoutPage_BindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is not BindableObject bindable)
            return;

        if (bindable.BindingContext is INotifyPropertyChanged notifiable)
        {
            notifiable.PropertyChanged -= ViewModel_PropertyChanged;
            notifiable.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(IFlyoutHost.IsPresented))
            return;

        if (_flyoutPage?.BindingContext is IFlyoutHost flyoutHost)
        {
            _flyoutPage.IsPresented = flyoutHost.IsPresented;
        }
    }
}