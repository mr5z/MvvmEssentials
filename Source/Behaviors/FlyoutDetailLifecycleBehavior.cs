using System.ComponentModel;
using Nkraft.CrossUtility.Extensions;
using Nkraft.MvvmEssentials.Services.Helpers;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;

namespace Nkraft.MvvmEssentials.Behaviors;

public class FlyoutDetailLifecycleBehavior : Behavior<FlyoutPage>
{
    private FlyoutPage? _flyoutPage;
    private bool _wasPresented;

    protected override void OnAttachedTo(FlyoutPage bindable)
    {
        base.OnAttachedTo(bindable);
        _flyoutPage = bindable;
        _wasPresented = bindable.IsPresented;
        
        bindable.Appearing += FlyoutPage_Appearing;
        bindable.Disappearing += FlyoutPage_Disappearing;
        bindable.PropertyChanged += FlyoutPage_PropertyChanged;
        
        if (bindable.Detail is not null)
        {
            TriggerDetailNavigatedTo(bindable.Detail);
        }
    }

    protected override void OnDetachingFrom(FlyoutPage bindable)
    {
        base.OnDetachingFrom(bindable);
        
        bindable.Appearing -= FlyoutPage_Appearing;
        bindable.Disappearing -= FlyoutPage_Disappearing;
        bindable.PropertyChanged -= FlyoutPage_PropertyChanged;
        
        _flyoutPage = null;
    }

    private void FlyoutPage_Appearing(object? sender, EventArgs e)
    {
        if (_flyoutPage?.Detail is { } detailPage)
        {
            PropagateAppearing(detailPage);
        }
    }

    private void FlyoutPage_Disappearing(object? sender, EventArgs e)
    {
        if (_flyoutPage?.Detail is { } detailPage)
        {
            PropagateDisappearing(detailPage);
        }
    }

    private void FlyoutPage_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not FlyoutPage flyoutPage)
            return;
        
        switch (e.PropertyName)
        {
            case nameof(FlyoutPage.Detail) when (flyoutPage.Detail is { } newDetail):
                TriggerDetailNavigatedTo(newDetail);
                break;
            
            case nameof(FlyoutPage.IsPresented):
            {
                if (_wasPresented && flyoutPage is { IsPresented: false, Detail: { } detailPage })
                {
                    PropagateAppearing(detailPage);
                }
            
                _wasPresented = flyoutPage.IsPresented;
                break;
            }
        }
    }

    private static void PropagateAppearing(Page page)
    {
        var targetPage = GetTargetPage(page);
        if (targetPage is null)
            return;
        
        if (targetPage.BindingContext is IAppearingAware appearing)
        {
            appearing.OnPageAppearing();
        }
        
        if (targetPage.BindingContext is IAppearingAwareAsync appearingAsync)
        {
            appearingAsync.OnPageAppearingAsync().FireAndForget(ex =>
            {
                ExceptionDispatcher.Handle<FlyoutDetailLifecycleBehavior>(ex, nameof(IAppearingAwareAsync.OnPageAppearingAsync));
            });
        }
        
        targetPage.SendAppearing();
    }

    private static void PropagateDisappearing(Page page)
    {
        var targetPage = GetTargetPage(page);
        if (targetPage is null)
            return;
        
        if (targetPage.BindingContext is IAppearingAware disappearing)
        {
            disappearing.OnPageDisappearing();
        }
        
        if (targetPage.BindingContext is IAppearingAwareAsync disappearingAsync)
        {
            disappearingAsync.OnPageDisappearingAsync().FireAndForget(ex =>
            {
                ExceptionDispatcher.Handle<FlyoutDetailLifecycleBehavior>(ex, nameof(IAppearingAwareAsync.OnPageDisappearingAsync));
            });
        }
        
        targetPage.SendDisappearing();
    }

    private static void TriggerDetailNavigatedTo(Page page)
    {
        var targetPage = GetTargetPage(page);

        if (targetPage?.BindingContext is INavigatedAware navigated)
        {
            navigated.OnNavigatedTo();
        }
    }
    
    private static Page? GetTargetPage(Page page)
    {
        return page is NavigationPage navPage ? navPage.CurrentPage : page;
    }

}