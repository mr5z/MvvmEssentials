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

### Returning a result on Cancel

Some modals change things as the user goes (a draft that saves itself, items added one by one), so the caller
must hear what happened however the modal is left. Say what the caller receives then with `SetCancelResult`,
each time it changes:

```cs
[RelayCommand]
private async Task Add(Item item)
{
    await _items.AddAsync(item);
    _added.Add(item);
    SetCancelResult(new Picked(_added));
}
```

From then on every row above that reads `Cancelled` hands the caller that value instead, including the iOS
swipe-down, which no view model is asked about. `CanDismissAsync` is asked where it was, and `Dismiss(result)`
still hands over its own result. Without `SetCancelResult`, nothing changes.

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
- **Don't stack modals without a reason.** A modal may present another one when it needs a result back from it,
  such as a picker: each has its own caller and its own result type. Otherwise close a modal before presenting
  another one. A popup on top is fine.

## Pages inside a modal

A modal can push further pages, which slide in inside it with their own top bar and Back. A page that is part of
the modal's task is a `ModalViewModel<TResult>` with the caller's result type, so any of them can finish the modal:

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
- **`ReplaceAsync(destination)`** pushes the next page, then removes the page it covers, so Back and `PopAsync()`
  skip it. Use it when the current page mustn't be gone back to, such as a finished task's outcome after the task
  (`ReplaceAsync<TNext>()` for a page without parameters). The covered page is removed a frame after the new page is
  on screen, so tearing down a large page can't delay the new page's first frame; the task completes after that.
  Replacing the first page makes the new page the first: it shows no back arrow, and Android Back there asks its
  `CanDismissAsync`.
- **Closing.** `Dismiss(result)` on any page closes the modal and hands over the result; `Dismiss()` on any
  page closes it with `Cancelled`, or with what any page set with
  [`SetCancelResult`](#returning-a-result-on-cancel). Popping a page doesn't affect the caller. If an earlier page's answer
  should stand whatever happens next, have the later page dismiss with it (`Dismiss(Draft!)`).
- **Where the stack comes from.** You present a page, not a `NavigationPage`: the `NavigationPage` is the
  [top bar](#top-bar) host, and pages are pushed onto it. Without it (`Modal.WithNavigation="False"`, as
  overlays use) the modal is a single page, and `PushAsync` fails with `NotSupported`.

## INavigationService while a modal is open

The modal on top covers every page behind it, so while a modal is open `INavigationService`'s relative navigation
applies to that modal, not to the hidden pages:

| Call | While a modal is open |
|---|---|
| `NavigateAsync("Page")` | Pushes the page inside the modal. `NotSupported` when the modal has no `NavigationPage` (`Modal.WithNavigation="False"`). |
| `NavigateBackAsync()` | Goes back one page inside the modal. `NotHandled` on its first page: leave a modal with `Dismiss`. |
| `NavigateToRootAsync()` | Goes back to the modal's first page. |

This is how a modal shows a page that isn't a `ModalViewModel<TResult>`: any `PageViewModel`, such as a detail
page the app also opens from elsewhere. The page stays what it is, and its own `NavigateAsync` and
`NavigateBackAsync` calls keep working inside the modal:

```cs
public partial class PickProductViewModel(IModalService modal, INavigationService navigation)
    : ModalViewModel<Product>(modal)
{
    // ProductDetailViewModel is a plain PageViewModel, also opened from the catalog
    [RelayCommand]
    private Task ShowDetail(Product product) => navigation.NavigateAsync(ProductDetailViewModel.With(id: product.Id));
}
```

Such a page can't finish the modal: going back from it returns to the page that pushed it, and the caller keeps
waiting. A page that should be able to hand over the result is a `ModalViewModel<TResult>`, pushed with
[`PushAsync`](#pages-inside-a-modal).

An absolute path (`/Home`) still replaces the main page. MAUI then drops the modal without popping it: the
caller receives `Cancelled`, and `CanDismissAsync` isn't asked.

---

See also: [NavigationPage](navigation-page.md) · [Popups](popups.md) · [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Wizard](wizard.md)
