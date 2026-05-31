# NavigationPage

Plain pages and the navigation service: how to move between pages, replace the stack, and pass
parameters. See the [main README](../README.md) for setup and the `MapPage` vs `RegisterPage`
distinction.

A page is navigatable once mapped:

```cs
registry.MapPage<LoginPage, LoginViewModel>()
    .MapPage<AccountPage, AccountViewModel>();
```

Its backing ViewModel derives from `PageViewModel` (see the lifecycle table in the
[main README](../README.md#viewmodel-lifecycle)).

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
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? ErrorMessage { get; set; }

    // Or handle manually
    protected override void OnParametersSet(INavigationParameters parameters)
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

---

See also: [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md) · [Popups](popups.md)
