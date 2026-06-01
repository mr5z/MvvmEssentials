# MvvmEssentials

Lightweight MVVM utility library for .NET MAUI that simplifies navigation, flyout menus, tabs, popups, and multi-step wizards with opinionated conventions and minimal boilerplate. An alternative to .NET MAUI Shell.

[![NuGet Version](https://img.shields.io/nuget/v/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![.NET](https://github.com/mr5z/MvvmEssentials/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mr5z/MvvmEssentials/actions/workflows/dotnet.yml)

## Page types

This page covers setup and the concepts shared by every page type. For usage of a specific
surface, see its guide:

- [NavigationPage](https://github.com/mr5z/MvvmEssentials/blob/main/docs/navigation-page.md) — plain pages and the navigation service (push/replace, parameters)
- [TabbedPage](https://github.com/mr5z/MvvmEssentials/blob/main/docs/tabbed-page.md) — bottom/top tabs with lifecycle propagation
- [FlyoutPage](https://github.com/mr5z/MvvmEssentials/blob/main/docs/flyout-page.md) — hamburger menu with a swappable detail area
- [Wizard](https://github.com/mr5z/MvvmEssentials/blob/main/docs/wizard.md) — multi-step flows over a shared state object
- [Popups](https://github.com/mr5z/MvvmEssentials/blob/main/docs/popups.md) — modal dialogs with result handling (powered by Mopups)

# Setup

Quick test using this [Food Delivery app](https://github.com/mr5z/ShowCase-FoodDelivery), or just follow the instructions below.

## 1. Configure in `MauiProgram.cs`

```cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureMvvmEssentials(registry =>
            {
                // ViewModel and Page naming convention must strictly be followed:
                // page_name + "Page", vm_name + "ViewModel", wherein page_name == vm_name
                //
                // Mark one page with isInitial: true. This is where the app starts.
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

That's it. The hook fires the initial navigation automatically.

## 3. Delete any `Shell` related files. They are not used here.

---

# Custom Startup Logic (Optional)

If you need to run async logic before deciding where to navigate (e.g. auth checks, feature flags),
implement `IAppStartup` anywhere in your project. The source generator will find it automatically,
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

# MapPage vs RegisterPage

Both methods register a ViewModel with a scoped lifetime in the DI container, but they serve different roles:
| | `MapPage<TPage, TViewModel>` | `RegisterPage<TViewModel>` |
|---|---|---|
| Navigatable via `NavigateAsync` | ✅ Yes | ❌ No |
| Creates a page–VM mapping | ✅ Yes | ❌ No |
| Scoped DI lifetime | ✅ Yes | ✅ Yes |
| Use for | Pages navigated to by the navigation service | VMs bound via XAML (tabs, flyout menu, wizard steps) |

Use `RegisterPage` for any ViewModel that is wired to its page or view via XAML `BindingContext`
rather than created by the navigation service. The navigation service has no knowledge of these VMs
and will not be able to navigate to them by name.

The flyout's initial detail is the one exception: it is bound in XAML like the others, but if your
app returns to it (for example, a "home" action in the menu), it must use `MapPage` so the navigation
service can find it again.
```cs
registry.MapPage<MainHostPage, MainHostViewModel>(isInitial: true) // navigatable
    .RegisterPage<MenuViewModel>()        // bound in XAML as FlyoutPage.Flyout
    // Ideally RegisterPage, but the flyout has to return to its initial detail page, which needs MapPage.
    .MapPage<MainTabbedPage, MainTabbedViewModel>() // bound in XAML as FlyoutPage.Detail
        .RegisterPage<HomeViewModel>()      // bound in XAML as a TabbedPage tab
        .RegisterPage<ProfileViewModel>()   // bound in XAML as a TabbedPage tab
    .MapPage<OrdersPage, OrdersViewModel>()      // navigatable
    .MapPage<SettingsPage, SettingsViewModel>(); // navigatable
```
> **Note:** The indentation above is cosmetic. `IPageRegistry` returns `this` from every call, so the
> chain is flat regardless of how it is formatted. Indent to reflect the conceptual parent–child
> relationship between a host page and its XAML-bound ViewModels.

---

# ViewModel Lifecycle

Every page-backing ViewModel derives from `PageViewModel` and shares the lifecycle below. Surface-specific
hooks (tabs, flyout menus, wizard steps) are documented in each page type's guide.

<details>
<summary>PageViewModel</summary>

| Method | When it is called |
|---|---|
| `OnParametersSet` | Called when navigation parameters are passed to this ViewModel |
| `OnInitialized` | Called once on the first page appearing |
| `OnInitializedAsync` | Async version of `OnInitialized` |
| `OnPageAppearing` | Called every time the page appears |
| `OnPageAppearingAsync` | Async version of `OnPageAppearing` |
| `OnPageDisappearing` | Called every time the page disappears |
| `OnPageDisappearingAsync` | Async version of `OnPageDisappearing` |
| `OnNavigatedTo` | Called when the page is navigated to |
| `OnNavigatedFrom` | Called when the page is navigated away from |
| `OnNavigatedToRoot` | Called when the navigation stack is popped back to this page as root |
| `OnNavigatedToRootAsync` | Async version of `OnNavigatedToRoot` |
| `OnPageUnloaded` | Called when the page is removed from the visual tree |
| `OnDispose` | Called when the DI scope is disposed |

</details>

---

# Resource Cleanup (IDisposable)

All ViewModels (`PageViewModel`, `TabViewModel`, `FlyoutMenuViewModel`, `PopupViewModel`, `WizardStepViewModel`) implement `IDisposable`. When a page is unloaded, the library automatically disposes its ViewModel via the DI scope.

To clean up resources, override `OnDispose()` in your ViewModel:

```cs
public class MyViewModel : PageViewModel
{
    private readonly Timer _timer;

    public MyViewModel()
    {
        _timer = new Timer(OnTick, null, 0, 1000);
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        _timer.Dispose();
    }
}
```

---

# Contributing

Contributions are welcome. To get started:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-change`)
3. Commit your changes (`git commit -am 'Add my change'`)
4. Push to the branch (`git push origin feature/my-change`)
5. Open a pull request

Please open an issue first for significant changes so the direction can be discussed before implementation.
