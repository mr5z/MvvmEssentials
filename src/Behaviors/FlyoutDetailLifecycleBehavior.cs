using System.ComponentModel;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

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

    // Exposing this internally as MAUI's SendAppearing()/SendDisappearing() doesn't trigger the events
    internal void FlyoutPage_Appearing(object? sender, EventArgs e)
    {
        if (_flyoutPage?.Detail is { } detailPage)
        {
            PropagateAppearing(detailPage);
        }
    }

    internal void FlyoutPage_Disappearing(object? sender, EventArgs e)
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
            case nameof(FlyoutPage.Detail):
                HandleDetailChanged(flyoutPage);
                break;
            case nameof(FlyoutPage.IsPresented):
                HandleIsPresentedChanged(flyoutPage);
                break;
        }
    }
    
    private static void HandleDetailChanged(FlyoutPage flyoutPage)
    {
        if (flyoutPage.Detail is { } newDetail)
            TriggerDetailNavigatedTo(newDetail);
    }

    private void HandleIsPresentedChanged(FlyoutPage flyoutPage)
    {
        if (_wasPresented && flyoutPage is { IsPresented: false, Detail: { } detailPage })
            PropagateAppearing(detailPage);

        _wasPresented = flyoutPage.IsPresented;
    }
    
    // Both methods manually fire native Appearing/Disappearing for FlyoutPage.Detail swaps
    // MAUI raises this automatically for normal push navigation (see PageFactory), but not here.
    private static void PropagateAppearing(Page page)
    {
        var targetPage = GetTargetPage(page);
        if (targetPage?.BindingContext is not IPageAppearing vm)
            return;
        
        ExceptionDispatcher.Invoke<FlyoutDetailLifecycleBehavior>(vm.OnPageAppearing, nameof(IPageAppearing.OnPageAppearing), vm);
        ExceptionDispatcher.InvokeAsync<FlyoutDetailLifecycleBehavior>(vm.OnPageAppearingAsync, nameof(IPageAppearing.OnPageAppearingAsync), vm);
        
        targetPage.SendAppearing();
    }

    private static void PropagateDisappearing(Page page)
    {
        var targetPage = GetTargetPage(page);
        if (targetPage?.BindingContext is not IPageAppearing vm)
            return;
        
        ExceptionDispatcher.Invoke<FlyoutDetailLifecycleBehavior>(vm.OnPageDisappearing, nameof(IPageAppearing.OnPageDisappearing), vm);
        ExceptionDispatcher.InvokeAsync<FlyoutDetailLifecycleBehavior>(vm.OnPageDisappearingAsync, nameof(IPageAppearing.OnPageDisappearingAsync), vm);
        
        targetPage.SendDisappearing();
    }

    private static void TriggerDetailNavigatedTo(Page page)
    {
        var targetPage = GetTargetPage(page);

        if (targetPage?.BindingContext is IPageNavigated vm)
        {
            ExceptionDispatcher.Invoke<FlyoutDetailLifecycleBehavior>(vm.OnPageNavigatedTo, nameof(IPageNavigated.OnPageNavigatedTo), vm);
            ExceptionDispatcher.InvokeAsync<FlyoutDetailLifecycleBehavior>(vm.OnPageNavigatedToAsync, nameof(IPageNavigated.OnPageNavigatedToAsync), vm);
        }
    }
    
    private static Page? GetTargetPage(Page page)
    {
        return page is NavigationPage navPage ? navPage.CurrentPage : page;
    }

}