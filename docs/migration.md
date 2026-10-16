# Migration guide

## 1.6.2 → next release

### Lifecycle exceptions crash by default

Exceptions thrown from ViewModel lifecycle methods (`OnInitialized`, `OnPageAppearing`, `OnNavigatedTo`, ...)
used to propagate into MAUI's navigation code, or, for async methods, were only logged in the NuGet package.
They are now always reported through one handler, and by default rethrown on the UI thread after the
navigation finishes, which normally terminates the app.

- To keep the previous "log and continue" behavior, set
  `options.LifecycleExceptionBehavior = LifecycleExceptionBehavior.LogOnly` in the new
  `ConfigureMvvmEssentials(registry => ..., options => ...)` overload.
- A navigation whose target page throws in `OnNavigatedTo`/`OnPageAppearing` now returns a successful
  `IResult`; the exception is reported through `OnLifecycleException` instead.

See [Lifecycle Exceptions](../README.md#lifecycle-exceptions).

### `IAppStartup.OnInitializedAsync` returns the navigation result

`Task OnInitializedAsync()` is now `Task<IResult> OnInitializedAsync()`. The library rethrows a failed
result's original exception, so you no longer need `result.ThrowIfFailure()`.

```cs
// Before
async Task IAppStartup.OnInitializedAsync()
{
    var result = await _navigationService.Absolute().Push<HomeViewModel>().NavigateAsync();
    result.ThrowIfFailure();
}

// After
async Task<IResult> IAppStartup.OnInitializedAsync()
{
    return await _navigationService.Absolute().Push<HomeViewModel>().NavigateAsync();
}
```

The compiler flags every implementation. The default startup generated from `isInitial: true` is updated
automatically. Returning `Result.Ok()` without navigating now throws an `InvalidOperationException` at startup.

## 1.5.1 → 1.6.0: Wizard steps use `MapView`

`CreateView<TView, TViewModel>()` is removed. Steps are now declared by ViewModel only with
`Defer<TViewModel>()`, and the view is mapped at registration. This lets wizard hosts live in a project that doesn't reference your views.

For each step:

1. In the host, change `Steps` to `IReadOnlyList<WizardStep>` and each
   `f => f.CreateView<OnboardingGoalView, OnboardingGoalViewModel>()` to `Defer<OnboardingGoalViewModel>()`.
2. In `ConfigureMvvmEssentials`, change `.RegisterPage<OnboardingGoalViewModel>()` to
   `.MapView<OnboardingGoalView, OnboardingGoalViewModel>()`.

The compiler flags every call in step 1. Step 2 is not checked: a missing `MapView` throws
`InvalidOperationException` when the wizard enters that step.

See [Wizard](wizard.md).

## 1.4.0.5 → 1.4.1.2: `Mopups` → `Nkraft.Mopups`

Popups now use [Nkraft.Mopups](https://www.nuget.org/packages/Nkraft.Mopups/), a maintained fork of
[Mopups](https://github.com/LuckyDucko/Mopups).

- Remove the `Mopups` package reference from your app. Having both causes CS0433.
- Remove your `.ConfigureMopups()` call. `ConfigureMvvmEssentials` already does this.
- Change `Mopups.*` namespaces to `Nkraft.Mopups.*`.
