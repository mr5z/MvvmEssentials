# Popups

Modal dialogs with result handling. This feature is made possible by the awesome
[Mopups](https://github.com/LuckyDucko/Mopups) library. See the [main README](../README.md) for
setup and the `MapPage` vs `RegisterPage` distinction.

## Setup

```cs
builder
    .UseMauiApp<App>()
    .ConfigureMopups(); // Required for Mopups

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

## Lifecycle

`PopupViewModel` inherits all lifecycle methods from `PageViewModel`
(see the [main README](../README.md#viewmodel-lifecycle)).

---

See also: [NavigationPage](navigation-page.md) · [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md)
