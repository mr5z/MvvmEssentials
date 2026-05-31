# FlyoutPage

A hamburger menu (the flyout) with a swappable detail area. The library propagates lifecycle events
to the detail page, which MAUI normally does not. See the [main README](../README.md) for setup and
the `MapPage` vs `RegisterPage` distinction.

**1. Register ViewModels in DI**

```cs
// The flyout menu and detail ViewModels are bound via XAML, so use RegisterPage.
// Pages navigated to from the menu are pushed via the navigation service, so use MapPage.
registry.MapPage<MainHostPage, MainHostViewModel>(isInitial: true)
    .RegisterPage<MenuViewModel>()
    .RegisterPage<MainTabbedViewModel>()
    .MapPage<OrdersPage, OrdersViewModel>()
    .MapPage<SettingsPage, SettingsViewModel>();
```

**2. Define the FlyoutPage in XAML**

```xaml
<FlyoutPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:behaviors="clr-namespace:Nkraft.MvvmEssentials.Behaviors;assembly=Nkraft.MvvmEssentials"
    xmlns:local="clr-namespace:MauiApp1"
    x:DataType="local:MainHostViewModel"
    x:Class="MauiApp1.MainHostPage">

    <FlyoutPage.Behaviors>
        <!-- Required for IsPresented to sync with the ViewModel -->
        <behaviors:FlyoutPresentingBehavior />
        <!-- Required for the detail page to receive lifecycle events -->
        <behaviors:FlyoutDetailLifecycleBehavior />
    </FlyoutPage.Behaviors>

    <FlyoutPage.Flyout>
        <local:MenuPage BindingContext="{Binding MenuViewModel}" Title="Menu" />
    </FlyoutPage.Flyout>

    <FlyoutPage.Detail>
        <local:MainTabbedPage BindingContext="{Binding DetailViewModel}" />
    </FlyoutPage.Detail>

</FlyoutPage>
```

**3. Define the flyout host ViewModel**

```cs
public sealed class MainHostViewModel(MenuViewModel menu, MainTabbedViewModel detail)
    : FlyoutHostViewModel<MenuViewModel, MainTabbedViewModel>(menu, detail);
```

**4. Define the menu ViewModel**

```cs
public partial class MenuViewModel(INavigationService navigationService) : FlyoutMenuViewModel
{
    private readonly INavigationService _navigationService = navigationService;

    protected override void OnFlyoutOpened()
    {
        base.OnFlyoutOpened();
        Console.WriteLine("Flyout opened");
    }

    protected override void OnFlyoutClosed()
    {
        base.OnFlyoutClosed();
        Console.WriteLine("Flyout closed");
    }
}
```

**5. Replace the detail page programmatically**

From within `FlyoutMenuViewModel`, call `ReplaceDetailAsync<TViewModel>` to swap the flyout's detail area by ViewModel type. The flyout is automatically dismissed after navigation.

Navigating to a ViewModel that matches the original detail (the one set in XAML) restores it directly rather than wrapping it in a new `NavigationPage`:

```cs
[RelayCommand]
private async Task NavigateToHome()
{
    // Restores the original detail page set in XAML
    await ReplaceDetailAsync<MainTabbedViewModel>(_navigationService);
}

[RelayCommand]
private async Task NavigateToOrders()
{
    await ReplaceDetailAsync<OrdersViewModel>(_navigationService);
}

[RelayCommand]
private async Task NavigateToSettings()
{
    var parameters = new NavigationParameters { { "Theme", "Dark" } };
    await ReplaceDetailAsync<SettingsViewModel>(_navigationService, parameters);
}
```

**6. Toggle the flyout programmatically**

```cs
// From MenuViewModel or MainHostViewModel
IsPresented = !IsPresented;
```

## Lifecycle

The host (`FlyoutHostViewModel<TMenu, TDetail>`) is a `PageViewModel` and follows the base
[lifecycle](../README.md#viewmodel-lifecycle). The menu's `FlyoutMenuViewModel` adds:

<details>
<summary>FlyoutMenuViewModel</summary>

| Method | When it is called |
|---|---|
| `OnFlyoutOpened` | Called every time the flyout is opened |
| `OnFlyoutOpenedAsync` | Async version of `OnFlyoutOpened` |
| `OnFlyoutClosed` | Called every time the flyout is closed |
| `OnFlyoutClosedAsync` | Async version of `OnFlyoutClosed` |
| `OnDispose` | Called when the parent host's DI scope is disposed |

</details>

---

See also: [NavigationPage](navigation-page.md) · [TabbedPage](tabbed-page.md) · [Wizard](wizard.md) · [Popups](popups.md)
