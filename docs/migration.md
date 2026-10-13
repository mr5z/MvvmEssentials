# Migration guide

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
