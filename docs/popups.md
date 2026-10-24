# Popups

Modal dialogs with result handling, built on [Nkraft.Mopups](https://www.nuget.org/packages/Nkraft.Mopups/),
a maintained fork of the [Mopups](https://github.com/LuckyDucko/Mopups) library. See the
[main README](../README.md) for setup and the `MapPage` vs `RegisterPage` distinction.

## Setup

No extra setup is needed. `ConfigureMvvmEssentials` configures Mopups for you, so do not call
`ConfigureMopups()` yourself. Upgrading from an older version? See
[Upgrading from versions that used `Mopups`](../README.md#upgrading-from-versions-that-used-mopups).

```cs
// Register popup pages alongside regular pages
registry.MapPage<ConfirmPopup, ConfirmViewModel>();
```

## Usage

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
}
```

> **Tip:** instead of building `NavigationParameters` by hand, mark `ConfirmViewModel`
> properties with `[NavigationParameter]` and call the generated
> `ConfirmViewModel.With(confirmationMessage: "Reset counter?")` — see
> [Strongly-typed navigation parameters](navigation-page.md#navigationextension-examples)
> for details.

## Dismissal

| Action | `CanDismissAsync` asked? | Caller receives |
|---|---|---|
| `Dismiss(result)` (Yes / No / Save) | No | the result |
| `DismissCommand` (close) | Yes | `Cancelled` |
| Background tap (`ShouldDismissOnBackgroundTapped`, default `true`) | Yes | `Cancelled` |
| Android Back (`ShouldDismissOnBackButtonPressed`, default `true`) | Yes | `Cancelled` |

Override `CanDismissAsync` to keep a popup open, for example while it is saving. Return `false` from the
`ShouldDismissOn...` properties for a tap or Back to do nothing at all.

Popups and [modals](modals.md#dismissal) dismiss the same way: both derive from `PresentedViewModel<TResult>`.

## Lifecycle

`PopupViewModel` inherits all lifecycle methods from `PageViewModel`
(see the [main README](../README.md#viewmodel-lifecycle)).

---

See also: [NavigationPage](navigation-page.md) · [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md)
