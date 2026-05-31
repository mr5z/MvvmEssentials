# TabbedPage

Bottom or top tabs, where each tab is a `NavigationPage` and tab selection is propagated to the
ViewModels as lifecycle events. See the [main README](../README.md) for setup and the `MapPage` vs
`RegisterPage` distinction.

**1. Register tabs in DI**

```cs
// Tab ViewModels are not mapped to pages. They are bound via XAML.
// Use RegisterPage to ensure they are registered with the correct lifetime.
registry.MapPage<MainPage, MainViewModel>()
    .RegisterPage<HomeViewModel>()
    .RegisterPage<SettingsViewModel>();
```

**2. Define tabs in XAML**

```xaml
<TabbedPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:behaviors="clr-namespace:Nkraft.MvvmEssentials.Behaviors;assembly=Nkraft.MvvmEssentials"
    xmlns:local="clr-namespace:MauiApp1"
    xmlns:android="clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;assembly=Microsoft.Maui.Controls"
    android:TabbedPage.ToolbarPlacement="Bottom"
    x:DataType="local:MainViewModel"
    x:Class="MauiApp1.MainPage">

    <!-- Required for tab selection via ViewModel to work -->
    <TabbedPage.Behaviors>
        <behaviors:TabSelectionBehavior />
    </TabbedPage.Behaviors>

    <NavigationPage Title="Home">
        <x:Arguments>
            <local:HomePage BindingContext="{Binding HomeViewModel}" />
        </x:Arguments>
    </NavigationPage>

    <NavigationPage Title="Settings">
        <x:Arguments>
            <local:SettingsPage BindingContext="{Binding SettingsViewModel}" />
        </x:Arguments>
    </NavigationPage>

</TabbedPage>
```

**3. Define the tab host ViewModel**

```cs
public class MainViewModel(HomeViewModel homeViewModel, SettingsViewModel settingsViewModel) : TabHostViewModel
{
    protected override IReadOnlyCollection<ITabComponent> Tabs => [HomeViewModel, SettingsViewModel];

    public HomeViewModel HomeViewModel { get; } = homeViewModel;
    public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;
}
```

**4. Define tab ViewModels**

```cs
public partial class HomeViewModel(ISemanticScreenReader screenReader) : TabViewModel
{
    private readonly ISemanticScreenReader _screenReader = screenReader;

    protected override void OnTabSelected()
    {
        base.OnTabSelected();
        Console.WriteLine("Home tab selected");
    }

    protected override void OnTabUnselected()
    {
        base.OnTabUnselected();
        Console.WriteLine("Bye!");
    }

    [RelayCommand]
    private void IncreaseCount()
    {
        Count++;
        CountButtonText = Count == 1 ? $"Clicked {Count} time" : $"Clicked {Count} times";
        _screenReader.Announce(CountButtonText);
    }

    public int Count { get; set; }
    public string CountButtonText { get; set; } = "Click me";
}
```

**5. Switch the current tab programmatically**

From within `TabHostViewModel`, call `SwitchTabAsync<TTabViewModel>` to switch to a tab by ViewModel type:

```cs
public class MainViewModel(
    HomeViewModel homeViewModel,
    SettingsViewModel settingsViewModel,
    INavigationService navigationService) : TabHostViewModel
{
    private readonly INavigationService _navigationService = navigationService;

    protected override IReadOnlyCollection<ITabComponent> Tabs => [HomeViewModel, SettingsViewModel];

    public HomeViewModel HomeViewModel { get; } = homeViewModel;
    public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;

    [RelayCommand]
    private async Task GoToSettings()
    {
        await SwitchTabAsync<SettingsViewModel>(_navigationService);
    }
}
```

## Lifecycle

The host (`TabHostViewModel`) is a `PageViewModel` and follows the base
[lifecycle](../README.md#viewmodel-lifecycle). Each tab's `TabViewModel` adds:

<details>
<summary>TabViewModel</summary>

| Method | When it is called |
|---|---|
| `OnInitialized` | Called once on the first tab selection |
| `OnInitializedAsync` | Async version of `OnInitialized` |
| `OnTabSelected` | Called every time the tab is selected |
| `OnTabSelectedAsync` | Async version of `OnTabSelected` |
| `OnTabUnselected` | Called every time the tab is unselected |
| `OnTabUnselectedAsync` | Async version of `OnTabUnselected` |
| `OnDispose` | Called when the parent host's DI scope is disposed |

</details>

---

See also: [NavigationPage](navigation-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md) · [Popups](popups.md)
