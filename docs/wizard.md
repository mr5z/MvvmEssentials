# Wizard

Multi-step flows (onboarding, checkout, setup) where several `ContentView` steps share a single
state object inside one host page. Steps are built lazily, cached after first visit, and given
enter/exit hooks to read from and write back to the shared state. See the [main README](../README.md)
for setup and the `MapPage` vs `RegisterPage` distinction.

The example below is a real three-step onboarding flow: **goal → experience → schedule**.

A wizard has three pieces:

| Piece | Base type | Role |
|---|---|---|
| Host | `WizardHostViewModel<TState>` | The page VM. Owns the step list, the shared `TState`, movement between steps, and completion. |
| Step | `WizardStepViewModel<TState>` | The VM behind each `ContentView` step. Writes its choice into the shared state on exit. |
| View factory | `IContentViewFactory` | Creates each step's `ContentView` + VM in its own DI scope. Injected into the host; registered automatically by `ConfigureMvvmEssentials`. |

## 1. Define the shared state

`TState` must have a public parameterless constructor; the host creates an
instance on construction. A `record` works well — each step returns an updated copy with `with`:

```cs
internal sealed record OnboardingState
{
    public TrainingGoal Goal { get; init; }
    public int ExperienceLevel { get; init; }
    public int DaysPerWeek { get; init; }
}
```

## 2. Define each step

A step is a `ContentView` whose `BindingContext` is a `WizardStepViewModel<TState>`. Override
`OnStepExited` to fold the step's choice into the shared state; it returns the (possibly new) state:

```cs
// Step 1 — goal. Writes the choice into the shared state; no navigation.
internal sealed partial class OnboardingGoalViewModel : WizardStepViewModel<OnboardingState>
{
    public TrainingGoal SelectedGoal { get; set; } = TrainingGoal.BuildMuscle;

    [RelayCommand]
    private void Choose(TrainingGoal goal) => SelectedGoal = goal;

    protected override OnboardingState OnStepExited(OnboardingState state) => state with
    {
        Goal = SelectedGoal
    };
}
```

The experience and schedule steps follow the same shape, each writing its own field
(`ExperienceLevel`, `DaysPerWeek`) on exit. The step contract is synchronous: `OnStepEntered`,
`OnStepExited` (returns the state), and `OnDispose` — all optional virtuals.

The matching view binds to the step VM's properties and commands:

```xaml
<ContentView ... x:DataType="vm:OnboardingGoalViewModel">
    <VerticalStackLayout Spacing="10">
        <Label Text="What's your main goal?" Style="{StaticResource DisplayLabel}" />
        <controls:SelectableOption Title="Build muscle" Subtitle="Hypertrophy · 8–12 reps"
                                   Value="{x:Static models:TrainingGoal.BuildMuscle}"
                                   Selected="{Binding SelectedGoal}" Command="{Binding ChooseCommand}" />
        <!-- ...more options... -->
    </VerticalStackLayout>
</ContentView>
```

> **Note:** `WizardStepViewModel<TState>` derives from `BaseViewModel`. Change notification for your
> own step properties (like `SelectedGoal`) works the same way it does elsewhere in your app (e.g.
> CommunityToolkit `[ObservableProperty]` or Fody).

## 3. Define the host

```cs
internal sealed partial class OnboardingHostViewModel(
    IContentViewFactory viewFactory,
    INavigationService navigationService,
    IProfileService profileService) : WizardHostViewModel<OnboardingState>(viewFactory)
{
    private readonly INavigationService _navigationService = navigationService;
    private readonly IProfileService _profileService = profileService;

    [RelayCommand] private Task Next() => GoNextAsync();
    [RelayCommand] private Task Back() => GoBackAsync();

    protected override IReadOnlyList<Func<IContentViewFactory, ContentView>> Steps =>
    [
        f => f.CreateView<OnboardingGoalView, OnboardingGoalViewModel>(),
        f => f.CreateView<OnboardingExperienceView, OnboardingExperienceViewModel>(),
        f => f.CreateView<OnboardingScheduleView, OnboardingScheduleViewModel>(),
    ];

    protected override async Task OnCompletedAsync()
    {
        // Runs when Next is invoked on the last step.
        await _profileService.CompleteOnboardingAsync(State.Goal);

        await _navigationService
            .Absolute(withNavigation: false)
            .Push<MainTabbedViewModel>()
            .NavigateAsync();
    }

    // Public, bindable chrome (see the note below for how these are kept in sync).
    public int StepIndex { get; private set; }
    public bool ShowBack { get; private set; }
    public string NextLabel { get; private set; } = "Continue  →";
    public string StepTitle { get; set; } = "Goal";
}
```

> **Important — surfacing the wizard's state to XAML.** `CurrentStep` is the only public member;
> `GoNextAsync`, `GoBackAsync`, `CurrentIndex`, `CanGoBack`, and `IsLastStep` are `protected`. The host
> *does* raise `PropertyChanged` for `CurrentStep`, `CurrentIndex`, `CanGoBack`, and `IsLastStep` as the
> wizard advances, so the clean pattern is to expose `[RelayCommand]` wrappers for the actions (above)
> and mirror the protected state into public properties by overriding `OnPropertyChanged`:
>
> ```cs
> protected override void OnPropertyChanged(PropertyChangedEventArgs args)
> {
>     base.OnPropertyChanged(args);
>     switch (args.PropertyName)
>     {
>         case nameof(CurrentIndex):
>             StepIndex = CurrentIndex;
>             UpdateStepTitle(CurrentIndex);
>             break;
>         case nameof(CanGoBack):
>             ShowBack = CanGoBack;
>             break;
>         case nameof(IsLastStep):
>             NextLabel = IsLastStep ? "Finish setup" : "Continue  →";
>             break;
>     }
> }
>
> private void UpdateStepTitle(int step) => StepTitle = step switch
> {
>     0 => "Goal", 1 => "Experience", 2 => "Schedule", _ => StepTitle
> };
> ```

## 4. Register the host and steps

The host is a `PageViewModel`, so map it like any page. Each step VM is resolved from DI by the view
factory (`GetRequiredService<TViewModel>()`), so it must be registered too — use `RegisterPage`:

```cs
registry.MapPage<OnboardingHostPage, OnboardingHostViewModel>()  // navigable host page
    .RegisterPage<OnboardingGoalViewModel>()
    .RegisterPage<OnboardingExperienceViewModel>()
    .RegisterPage<OnboardingScheduleViewModel>();
```

> **Note:** Step views are created via `CreateView<TContentView, TViewModel>()` with both types passed
> explicitly, so the `{Name}Page` / `{Name}ViewModel` naming convention does **not** apply to steps —
> name your views (e.g. `OnboardingGoalView`) and step VMs however you like.

## 5. Host the current step in XAML

The host page is an ordinary `ContentPage`. Bind a `ContentView` to `CurrentStep` for the step area,
and bind your own chrome (progress, Back/Next, title) to the public properties from step 3:

```xaml
<ContentPage ... x:DataType="vm:OnboardingHostViewModel" Title="{Binding StepTitle}">
    <Grid RowDefinitions="Auto,*,Auto" Padding="22,28">

        <!-- Progress dots driven by StepIndex (light up as steps are reached) -->
        <Grid Grid.Row="0" ColumnDefinitions="*,*,*" ColumnSpacing="6">
            <BoxView Color="{StaticResource Primary}" HeightRequest="4" CornerRadius="3" />
            <BoxView Grid.Column="1" Color="{StaticResource Divider}" HeightRequest="4" CornerRadius="3">
                <BoxView.Triggers>
                    <DataTrigger TargetType="BoxView" Binding="{Binding StepIndex}" Value="{x:Int32 1}">
                        <Setter Property="Color" Value="{StaticResource Primary}" />
                    </DataTrigger>
                    <DataTrigger TargetType="BoxView" Binding="{Binding StepIndex}" Value="{x:Int32 2}">
                        <Setter Property="Color" Value="{StaticResource Primary}" />
                    </DataTrigger>
                </BoxView.Triggers>
            </BoxView>
            <!-- third dot: same pattern, active at StepIndex == 2 -->
        </Grid>

        <!-- Current step content (host swaps this as the wizard advances) -->
        <ContentView Grid.Row="1" Content="{Binding CurrentStep}" />

        <!-- Back / Next -->
        <Grid Grid.Row="2" ColumnDefinitions="Auto,*" ColumnSpacing="12">
            <Button Text="Back" IsVisible="{Binding ShowBack}" Command="{Binding BackCommand}" />
            <Button Grid.Column="1" Text="{Binding NextLabel}" Command="{Binding NextCommand}" />
        </Grid>

    </Grid>
</ContentPage>
```

## Behavior notes

- The first step is built and entered when the host page first appears (via `OnInitialized`).
- Each step's `ContentView` and VM are created once and **cached**; revisiting a step reuses the same
  instance (the enter/exit hooks still fire on each visit).
- Calling `GoNextAsync` on the last step commits the current step (`OnStepExited`), then invokes
  `OnCompletedAsync` instead of advancing.
- `GoNextAsync` silently no-ops when `CanAdvanceFrom(CurrentIndex)` is `false` (override it to gate a
  step); it does not auto-disable your Next button — bind enablement yourself if you want that.
- `GoBackAsync` returns a `Task` for call-site consistency but completes synchronously.
- When the host's DI scope is disposed, the view factory disposes every cached step scope (and any
  `IDisposable` step VM).

## Lifecycle

The host is a `PageViewModel` and follows the base [lifecycle](../README.md#viewmodel-lifecycle). Each
step's `WizardStepViewModel` adds:

<details>
<summary>WizardStepViewModel</summary>

| Method | When it is called |
|---|---|
| `OnStepEntered` | Every time the step becomes the current step |
| `OnStepExited` | Every time the wizard leaves the step (including before completion on the last step); returns the mutated state |
| `OnDispose` | Called when the host's view factory is disposed |

</details>

---

See also: [NavigationPage](navigation-page.md) · [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Popups](popups.md)
