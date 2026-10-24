# Modals

Pages presented with `IModalService` that return a result. See the [main README](../README.md) for setup.

| Need | Use |
|---|---|
| Short decision (confirm, choose one) | [Popups](popups.md) |
| Next step in the same flow | [NavigationPage](navigation-page.md) push |
| Separate task that returns a result (edit address, compose, pick) | **Modal** |

## Usage

**1. Register the page:**

```cs
registry.MapPage<EditAddressPage, EditAddressViewModel>();
```

**2. Define the page.** Any page works; put the title and actions on it:

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    x:Class="MyApp.EditAddressPage"
    x:DataType="local:EditAddressViewModel"
    Title="Edit address">

    <ContentPage.ToolbarItems>
        <ToolbarItem Text="Cancel" Command="{Binding DismissCommand}" />
        <ToolbarItem Text="Save" Command="{Binding SaveCommand}" />
    </ContentPage.ToolbarItems>

    <!-- content -->
</ContentPage>
```

**3. Back it with `ModalViewModel<TResult>`:**

```cs
public partial class EditAddressViewModel(IModalService modalService, IDialogService dialogs)
    : ModalViewModel<DeliveryAddress>(modalService)
{
    [RelayCommand]
    private Task Save() => Dismiss(new DeliveryAddress(/* ... */));

    // Optional: ask before discarding changes
    protected override async Task<bool> CanDismissAsync()
        => !HasChanges || await dialogs.ConfirmAsync("Discard changes?");
}
```

**4. Present it and await the result:**

```cs
var result = await _modalService.PresentAsync<EditAddressViewModel, DeliveryAddress>();
if (result.TryGetValue(out var address))
{
    // saved
}
// ErrorCode.Cancelled when the user cancelled, pressed Back, or swiped an iOS sheet away
```

## Top bar

By default the page is hosted in a `NavigationPage`, so it gets the same top bar as your regular pages,
styled by your app's implicit `NavigationPage` style. On Android this bar also fills the area behind
the status bar, which is transparent on Android 15 and later. Put the title and actions on the page
(`Title`, `ToolbarItems`), as in the example above.

For a modal that draws its own background, opt out:

```xml
<ContentPage ...
    xmlns:nkraft="clr-namespace:Nkraft.MvvmEssentials.Pages;assembly=Nkraft.MvvmEssentials"
    nkraft:Modal.WithNavigation="False">
```

The page then draws the area behind the status bar itself.

## Presentation styles

The library adds no presentation setting of its own; the page's platform settings are used as-is.

| Style (Apple / Material) | How |
|---|---|
| Full screen / full-screen dialog | Default. |
| Sheet (iOS) | `ios:Page.ModalPresentationStyle="PageSheet"` on the page. Swipe-down dismisses it with `Cancelled`. On Android it shows full screen. |
| Overlay (dimmed backdrop, card, custom sheet) | `nkraft:Modal.WithNavigation="False"`, a transparent or translucent `BackgroundColor`, and on iOS `ios:Page.ModalPresentationStyle="OverFullScreen"`. |

`ios:Page.ModalPresentationStyle` is set on your page and still applies when the page is hosted in a
`NavigationPage`: the library passes it on to the host page, because MAUI reads it from the page it pushes.

## Dismissal

| Action | `CanDismissAsync` asked? | Caller receives |
|---|---|---|
| `Dismiss(result)` (Save / Done) | No | the result |
| `DismissCommand` (Cancel / close) | Yes | `Cancelled` |
| Android Back | Yes | `Cancelled` |
| iOS sheet swipe-down | No (MAUI can't intercept it) | `Cancelled` |

Pressing Back again while a confirmation is open doesn't start a second one.

Close modals with `IModalService` or the view model's `Dismiss`. On Android, any other pop of a modal
backed by a `ModalViewModel`, including calling `Navigation.PopModalAsync()` directly, is treated like
Back and goes through `CanDismissAsync`; on iOS it isn't.

## Guidelines

Apple's and Google's guidance for modal tasks, and how to follow it here:

- **Name the task.** Set `Title`.
- **Give a clear way out.** iOS: Cancel on the leading edge, Done/Save on the trailing edge. Android: close (X)
  and Save in the top bar. Bind Cancel/close to `DismissCommand`.
- **Don't lose work silently.** Override `CanDismissAsync` when the modal holds unsaved input.
- **Keep it to one task.** A modal can hold several pages ([below](#pages-inside-a-modal)), but they should all
  serve the task it was opened for. A [wizard](wizard.md#presenting-the-wizard-modally-optional) keeps every
  step inside one page, which also works in overlays.
- **One modal at a time.** Close a modal before presenting another one; a popup on top is fine.

## Pages inside a modal

A modal can push further pages, which slide in inside it with their own top bar and Back. Every page is a
`ModalViewModel<TResult>` with the caller's result type, so any of them can finish the modal:

```cs
// Caller: unchanged
var result = await _modalService.PresentAsync(EditAddressViewModel.With(street: "123 Main St"));

public partial class EditAddressViewModel(IModalService modal) : ModalViewModel<DeliveryAddress>(modal)
{
    [RelayCommand]
    private Task Next() => PushAsync(ConfirmAddressViewModel.With(draft: new DeliveryAddress(/* ... */)));
}

public partial class ConfirmAddressViewModel(IModalService modal) : ModalViewModel<DeliveryAddress>(modal)
{
    [NavigationParameter] public DeliveryAddress? Draft { get; set; }

    [RelayCommand] private Task Edit() => PopAsync();          // back to EditAddress
    [RelayCommand] private Task Confirm() => Dismiss(Draft!);  // the caller receives it
}
```

- **`PushAsync(destination)`** pushes the next page; pass its data through its navigation parameters
  (`With(...)`). It only accepts a destination of the same result type, so a page that returns something else
  doesn't compile. `PushAsync<TNext>()` pushes a page that has no parameters.
- **`PopAsync()`** goes back one page, as do the back arrow and Android Back. On the first page it returns
  `NotHandled`; there, Android Back asks `CanDismissAsync`.
- **Closing.** `Dismiss(result)` on any page closes the modal and hands over the result; `Dismiss()` on any
  page closes it with `Cancelled`. Popping a page doesn't affect the caller. If an earlier page's answer
  should stand whatever happens next, have the later page dismiss with it (`Dismiss(Draft!)`).
- **Where the stack comes from.** You present a page, not a `NavigationPage`: the `NavigationPage` is the
  [top bar](#top-bar) host, and pages are pushed onto it. Without it (`Modal.WithNavigation="False"`, as
  overlays use) the modal is a single page, and `PushAsync` fails with `NotSupported`.

## INavigationService while a modal is open

`INavigationService` navigates the pages behind the modal, which are hidden, so while a modal is open its
relative navigation (`NavigateAsync("Page")`, `NavigateBackAsync`, `NavigateToRootAsync`) fails with
`NotSupported`. Navigate inside the modal from its view models, or dismiss it first.

An absolute path (`/Home`) still replaces the main page. MAUI then drops the modal without popping it: the
caller receives `Cancelled`, and `CanDismissAsync` isn't asked.

---

See also: [NavigationPage](navigation-page.md) · [Popups](popups.md) · [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md)