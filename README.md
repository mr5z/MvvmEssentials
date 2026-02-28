# MvvmEssentials

Lightweight MVVM utility library for .NET MAUI. It simplifies navigation, tab handling, and popup management with opinionated conventions and minimal boilerplate. It also serves as an alternative to .NET MAUI Shell.

[![NuGet Version](https://img.shields.io/nuget/v/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![NuGet Pre-release](https://img.shields.io/nuget/vpre/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![GitHub Release](https://img.shields.io/github/release/mr5z/MvvmEssentials.svg?style=flat)](https://github.com/mr5z/MvvmEssentials/packages/385702)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![.NET](https://github.com/mr5z/MvvmEssentials/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mr5z/MvvmEssentials/actions/workflows/dotnet.yml)

# Setup

Quick test using this [test project](https://github.com/mr5z/MauiTest1), or just follow the instructions below.

## 1. Configure in `MauiProgram.cs`

```cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureMvvmEssentials(Assembly.GetExecutingAssembly(), registry =>
            {
                // ViewModel and Page naming convention must strictly be followed:
                // page_name + "Page", vm_name + "ViewModel", wherein page_name == vm_name
                //
                // Mark one page with isInitial: true — this is where the app starts.
                // If you need conditional startup logic (e.g. auth checks),
                // implement IAppStartup instead (see below).
                registry.MapPage<LandingPage, LandingViewModel>(isInitial: true)
                    .MapPage<MainPage, MainViewModel>()
                    .MapPage<LoginPage, LoginViewModel>()
                    .MapPage<SettingsPage, SettingsViewModel>();
            });

        // Required: registers the auto-discovered or generated IAppStartup
        builder.Services.AddDiscoveredAppStartup();

        // ..
    }
}
```

> **Note:** `AddDiscoveredAppStartup()` is source-generated at compile time. It automatically wires up
> your `IAppStartup` implementation if one exists, or generates a default one from the page marked
> `isInitial: true`.

## 2. Wire up the window in `App.xaml.cs`

```cs
public partial class App : Application
{
    private readonly AppStartupWindowHook _hook;

    public App(AppStartupWindowHook hook)
    {
        _hook = hook;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _hook.Attach();
        return base.CreateWindow(activationState);
    }
}
```

That's it. The hook handles all window lifecycle events and fires the initial navigation automatically.

## 3. Delete any `Shell` related files. They are not used here.

---

# Custom Startup Logic (Optional)

If you need to run async logic before deciding where to navigate (e.g. auth checks, feature flags),
implement `IAppStartup` anywhere in your project. The source generator will find it automatically —
no registration needed.

```cs
public class AppStartup : IAppStartup
{
    private readonly INavigationService _navigationService;
    private readonly IAuthService _authService;

    public AppStartup(INavigationService navigationService, IAuthService authService)
    {
        _navigationService = navigationService;
        _authService = authService;
    }

    public async Task OnInitializedAsync()
    {
        var isLoggedIn = await _authService.CheckAsync();

        await _navigationService
            .Absolute(withNavigation: false)
            .Push(isLoggedIn ? typeof(HomeViewModel) : typeof(LoginViewModel))
            .NavigateAsync();
    }
}
```

> **Rules:**
> - Only one `IAppStartup` implementation is allowed. Defining two will produce a **compile error (MVE002)**.
> - If no `IAppStartup` is found and no page is marked `isInitial: true`, a **compiler warning (MVE001)** is emitted.
> - When `IAppStartup` is defined, the `isInitial: true` flag is ignored.

---

# Usage

## NavigationService

```cs
interface INavigationService
{
    // Under the hood, detects which current page type is active and performs either
    // a page replacement or pushes onto the stack if it's a NavigationPage.
    // Prefer the extensions below over calling this directly.
    Task<IResult> NavigateAsync(string path, INavigationParameters? parameters = null, bool animated = true);

    // Wraps Navigation.PopAsync()
    Task<IResult> NavigateBackAsync(bool animated = true);

    // Wraps Navigation.PopToRootAsync()
    Task<IResult> NavigateToRootAsync(INavigationParameters? parameters = null, bool animated = true);
}
```

### NavigationExtension Examples

**1. Absolute navigation (page replacement)**

```cs
await _navigationService.Absolute(withNavigation: true)
    .Push<FirstViewModel, object>(new { A = 1 }) // .Push can only handle "primitive" data types
    .Push<SecondViewModel, object>(new { B = 2 })
    .Push<ThirdViewModel, object>(new { C = 3 })
    .NavigateAsync();
// Constructs "//NavigationPage/FirstPage?A=1/SecondPage?B=2/ThirdPage?C=3"
```

**2. Navigation with parameters**

```cs
// Pass parameters via object type
await _navigationService.NavigateAsync<LoginViewModel, object>(new { ErrorMessage = "Session expired", Test = 1 });

// Pass parameters via custom type
record LoginParameters(string ErrorMessage, int Test);
await _navigationService.NavigateAsync<LoginViewModel, LoginParameters>(new("Session expired", 1));

// Pass parameters via INavigationParameters
var parameters = new NavigationParameters
{
    { "ErrorMessage", "Session expired" },
    { "Test", 1 }
};
await _navigationService.NavigateAsync<LoginViewModel>(parameters);

// LoginViewModel.cs
class LoginViewModel : PageViewModel
{
    // Automatically mapped from navigation parameters
    public string? ErrorMessage { get; set; }

    // Or handle manually
    public override void OnParametersSet(INavigationParameters parameters)
    {
        if (parameters.TryGetValue<int>("Test", out var testValue))
        {
            // ..
        }
    }
}
```

**3. Contextual navigation**

```cs
// Replaces the page if the active page is not a NavigationPage,
// or pushes onto the stack if it is.
await _navigationService.NavigateAsync<AccountViewModel>();
```

**4. Select tab of TabbedPage**

```cs
await _navigationService.Absolute(withNavigation: false)
    .Push<MainViewModel, object>(new { SelectedTabIndex = 2 }) // switches to 3rd tab
    .NavigateAsync();
```

---

## TabbedPage + NavigationPage

**1. Register tabs in DI**

```cs
// Tab ViewModels are not mapped to pages — they are bound via XAML
builder.Services.AddTransient<HomeViewModel>();
builder.Services.AddTransient<SettingsViewModel>();

// Only the tab host is mapped
registry.MapPage<MainPage, MainViewModel>();
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
    public override IReadOnlyCollection<ITabComponent> Tabs => [HomeViewModel, SettingsViewModel];

    public HomeViewModel HomeViewModel { get; } = homeViewModel;
    public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;
}
```

**4. Define tab ViewModels**

```cs
public partial class HomeViewModel(ISemanticScreenReader screenReader) : TabViewModel
{
    private readonly ISemanticScreenReader _screenReader = screenReader;

    public override void OnTabSelected()
    {
        base.OnTabSelected();
        Console.WriteLine("Home tab selected");
    }

    public override void OnTabUnselected()
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

---

## FlyoutPage

```cs
// TODO
```

---

## PopupPage

This feature is made possible by the awesome [Mopups](https://github.com/LuckyDucko/Mopups) library.

### Setup

```cs
builder
    .UseMauiApp<App>()
    .ConfigureMopups(); // Required for Mopups

// Register popup pages alongside regular pages
registry.MapPage<ConfirmPopup, ConfirmViewModel>();
```

### Usage

**1. Define the popup in XAML**

```xaml
<nkraft:PopupPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:nkraft="clr-namespace:Nkraft.MvvmEssentials.Pages;assembly=Nkraft.MvvmEssentials"
    x:DataType="local:ConfirmViewModel"
    x:Class="MauiApp1.ConfirmPopup"
    Title="Confirm">

    <!-- layout omitted for brevity -->
</nkraft:PopupPage>
```

**2. Define the backing ViewModel**

```cs
record ConfirmResult(bool Confirm);

internal partial class ConfirmViewModel(IPopupService popupService) : PopupViewModel<ConfirmResult>(popupService)
{
    [RelayCommand]
    private async Task Yes() => await Dismiss(new ConfirmResult(true));

    [RelayCommand]
    private async Task No() => await Dismiss(new ConfirmResult(false));

    public string? ConfirmationMessage { get; set; }
}
```

**3. Present the popup from another ViewModel**

```cs
var navParams = new NavigationParameters
{
    { nameof(ConfirmViewModel.ConfirmationMessage), "Reset counter?" }
};

var result = await _popupService.PresentAsync<ConfirmViewModel, ConfirmResult>(navParams);

if (result.TryGetValue(out var confirmResult))
{
    Console.WriteLine("User pressed: {0}", confirmResult.Confirm ? "Yes" : "No");
    if (confirmResult.Confirm)
        Count = 0;
}
else
{
    // User tapped the background or pressed back
    Console.WriteLine("User cancelled the popup");
}
```

---

# Notes

- Inspired by [Prism](https://github.com/PrismLibrary/Prism)

# Contribution

Pull requests and issues are welcome. Thanks!
