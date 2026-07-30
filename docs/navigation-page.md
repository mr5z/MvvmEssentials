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

**3. Strongly-typed navigation parameters (`[NavigationParameter]`)**

Instead of building `NavigationParameters` by hand, mark ViewModel properties with
`[NavigationParameter]` and the source generator emits a static `With(...)` factory you
can pass straight to `NavigateAsync`:

> **Note:** the ViewModel must derive from `PageViewModel` (or `PopupViewModel<T>`, see
> below). Otherwise `With(...)` isn't generated, and you'll get a compiler warning
> (**MVE006**) pointing at the class.

```cs
public partial class LoginViewModel : PageViewModel
{
    [NavigationParameter]
    public string? ErrorMessage { get; set; }

    [NavigationParameter(IsOptional = true)]
    public int RetryCount { get; set; }
}

await _navigationService.NavigateAsync(
    LoginViewModel.With(errorMessage: "Session expired", retryCount: 2));
```

- The ViewModel must be declared `partial`, or the `With(...)` factory can't be generated.
- `IsOptional = true` gives the generated parameter a `default` value and moves it to the
  end of the parameter list (required parameters are emitted first).
- `PreferredName` renames the generated `With(...)` parameter for call-site ergonomics
  only — the navigation-parameter dictionary key is still the property name:
  ```cs
  [NavigationParameter(PreferredName = "id")]
  public int ItemId { get; set; }
  // -> LoginViewModel.With(id: 5)
  ```
- `[NavigationParameter]` properties declared on a base `PageViewModel` are collected too;
  if a derived class re-declares the same property, it's emitted once (most-derived wins).
- A `PreferredName` must be a valid C# identifier, and two properties can't resolve to
  the same parameter name — both will fail to compile.

The same attribute works on popups, generating a `With(...)` you can pass straight to
`PresentAsync` (see [Popups](popups.md)):

```cs
var result = await _popupService.PresentAsync(
    ConfirmViewModel.With(confirmationMessage: "Reset counter?"));
```

The same `With(...)` result also works with the `Absolute()`/`Relative()` fluent chain:

```cs
await _navigationService.Absolute(withNavigation: true)
    .Push(FirstViewModel.With(a: 1))
    .Push(SecondViewModel.With(b: 2))
    .NavigateAsync();
```

**4. Contextual navigation**

```cs
// Replaces the page if the active page is not a NavigationPage,
// or pushes onto the stack if it is.
await _navigationService.NavigateAsync<AccountViewModel>();
```

---

See also: [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md) · [Popups](popups.md)
