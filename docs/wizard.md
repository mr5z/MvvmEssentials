# Wizard

Multi-step flows (onboarding, checkout, setup): several `ContentView` steps sharing one state object
inside a single host page. Steps are built lazily, cached after the first visit, and given enter/exit
hooks to read from and write back to the shared state.

See the [main README](../README.md) for setup and the `MapPage` vs `RegisterPage` vs `MapView` distinction.

## The three pieces

| Piece | Base type | Role |
|---|---|---|
| Host | `WizardHostViewModel<TState>` | The page VM. Owns the step list, the shared `TState`, movement between steps, and completion. |
| Step | `WizardStepViewModel<TState>` | The VM behind each `ContentView` step. Writes its choice into the shared state on exit. |
| View factory | `IContentViewFactory` | Creates each step's `ContentView` + VM in its own DI scope, looking the view up from the VM type. Injected into the host; registered automatically by `ConfigureMvvmEssentials`. |

The example throughout is a three-step onboarding flow: **goal → experience → schedule**.

---

## 1. Define the shared state

`TState` needs a public parameterless constructor — the host creates an instance on construction.
A `record` works well, since each step can return an updated copy with `with`:

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
`OnStepExited` to fold the step's choice into the shared state:

```cs
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

The other two steps follow the same shape, each writing its own field on exit.

The step's view binds to it like any other `ContentView`:

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

> **Note:** `WizardStepViewModel<TState>` derives from `BaseViewModel`, which implements
> `INotifyPropertyChanged`. Bindable properties you add yourself (like `SelectedGoal`) need change
> notification raised for the UI to update — wire it up the same way you do elsewhere in your app.

### Requiring an answer (optional)

Override `CanAdvance` to keep the wizard on a step until it is answered. For example, with no
preselected goal:

```cs
public TrainingGoal? SelectedGoal { get; set; }

protected override bool CanAdvance => SelectedGoal is not null;

// Also runs when going Back from an unanswered step, so don't assume an answer here
protected override OnboardingState OnStepExited(OnboardingState state) => state with
{
    Goal = SelectedGoal ?? state.Goal
};
```

The host re-reads `CanAdvance` when the step is entered and whenever the step raises `PropertyChanged`
for **any** property, so no property names are involved. Read only state that raises `PropertyChanged`,
which is the same requirement as for the step's own bindings. `CanAdvance` also gates completion on
the last step.

## 3. Define the host

List the steps, handle completion, and expose commands for Next/Back:

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

    protected override IReadOnlyList<WizardStep> Steps =>
    [
        StepOf<OnboardingGoalViewModel>(),
        StepOf<OnboardingExperienceViewModel>(),
        StepOf<OnboardingScheduleViewModel>(),
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
}
```

`StepOf<TViewModel>()` declares a step without creating it. Its view and VM are created the first time
the wizard enters the step. `TViewModel` must be a `WizardStepViewModel<TState>` for this host's
state, so listing the wrong VM is a compile error.

The host names only ViewModels, so it can live in a separate project (e.g. `Core`) that does not
reference the project holding your views (e.g. `App`). The view for each VM is supplied at
registration, next.

That is the whole host — its state is already bindable, as the next step shows.

## 4. Register the host and steps

The host is a `PageViewModel`, so map it like any page. Map each step's view to its VM with
`MapView` — this tells the view factory which `ContentView` to build for the VM, and registers the
VM in DI with a scoped lifetime:

```cs
registry.MapPage<OnboardingHostPage, OnboardingHostViewModel>()  // navigable host page
    .MapView<OnboardingGoalView, OnboardingGoalViewModel>()
    .MapView<OnboardingExperienceView, OnboardingExperienceViewModel>()
    .MapView<OnboardingScheduleView, OnboardingScheduleViewModel>();
```

> **Note:** `MapView` has no naming convention — name your views and step VMs however you like.
> Each VM can be mapped to one view only; mapping it twice throws at startup.

> **Note:** A step whose VM is not mapped throws `InvalidOperationException` when the wizard first
> enters it, not at startup. Map every step.

## 5. Build the host page in XAML

The host page is an ordinary `ContentPage`. Bind a `ContentView` to `CurrentStep` for the step area,
and bind your chrome directly to the host's state:

```xaml
<ContentPage ... x:DataType="vm:OnboardingHostViewModel">
    <Grid RowDefinitions="Auto,*,Auto" Padding="22,28">

        <!-- Current step content (the host swaps this as the wizard advances) -->
        <ContentView Grid.Row="1" Content="{Binding CurrentStep}" />

        <!-- Back / Next -->
        <Grid Grid.Row="2" ColumnDefinitions="Auto,*" ColumnSpacing="12">
            <Button Text="Back" IsVisible="{Binding CanGoBack}" Command="{Binding BackCommand}" />
            <Button Grid.Column="1" Text="Continue" Command="{Binding NextCommand}" />
        </Grid>

    </Grid>
</ContentPage>
```

### What the host exposes

All of these raise `PropertyChanged` as the wizard advances, so you can bind to them directly:

| Member | Type | Use it for |
|---|---|---|
| `CurrentStep` | `ContentView?` | The step content area |
| `CurrentIndex` | `int` | Progress indicators, step counters |
| `CanGoBack` | `bool` | Showing or enabling the Back button |
| `IsLastStep` | `bool` | Switching the Next button to "Finish" |
| `CanGoNext` | `bool` | Enabling the Next button (`false` while the current step can't advance) |

To disable Next while the current step is unanswered, gate the command on `CanGoNext`:

```cs
[RelayCommand(CanExecute = nameof(CanGoNext))] private Task Next() => GoNextAsync();

protected override void OnPropertyChanged(PropertyChangedEventArgs args)
{
    base.OnPropertyChanged(args);

    if (args.PropertyName == nameof(CanGoNext))
        NextCommand.NotifyCanExecuteChanged();
}
```

For example, a progress dot that lights up on the second step:

```xaml
<BoxView Color="{StaticResource Divider}" HeightRequest="4" CornerRadius="3">
    <BoxView.Triggers>
        <DataTrigger TargetType="BoxView" Binding="{Binding CurrentIndex}" Value="{x:Int32 1}">
            <Setter Property="Color" Value="{StaticResource Primary}" />
        </DataTrigger>
    </BoxView.Triggers>
</BoxView>
```

### Deriving your own chrome (optional)

For app-specific text like a page title or a Next button label, override `OnPropertyChanged` and
react to the host's state:

```cs
public string NextLabel { get; private set; } = "Continue";
public string StepTitle { get; private set; } = "Goal";

protected override void OnPropertyChanged(PropertyChangedEventArgs args)
{
    base.OnPropertyChanged(args);

    switch (args.PropertyName)
    {
        case nameof(CurrentIndex):
            StepTitle = CurrentIndex switch
            {
                0 => "Goal", 1 => "Experience", 2 => "Schedule", _ => StepTitle
            };
            break;
        case nameof(IsLastStep):
            NextLabel = IsLastStep ? "Finish setup" : "Continue";
            break;
    }
}
```

> These are your own properties, so they need change notification raised for the bindings to update.

---

## Behavior notes

- The first step is built and entered when the host page first appears (via `OnInitialized`).
- Each step's `ContentView` and VM are created once and **cached**; revisiting a step reuses the same
  instance, though the enter/exit hooks still fire on each visit.
- `GoNextAsync` on the last step commits the current step (`OnStepExited`), then invokes
  `OnCompletedAsync` instead of advancing.
- `GoNextAsync` silently no-ops when `CanAdvanceFrom(CurrentIndex)` is `false`. By default that is the
  current step's `CanAdvance`. It does not auto-disable your Next button; gate it on `CanGoNext` (above).
- Override `CanAdvanceFrom` only for rules that live on the host. `CanGoNext` is re-read whenever the
  host or the current step raises `PropertyChanged`, so read only state that does.
- `GoBackAsync` returns a `Task` for call-site consistency but completes synchronously.
- When the host's DI scope is disposed, the view factory disposes every cached step scope, along with
  any `IDisposable` step VM.

## Lifecycle

The host is a `PageViewModel` and follows the base [lifecycle](../README.md#viewmodel-lifecycle). Each
step's `WizardStepViewModel` adds:

<details>
<summary>WizardStepViewModel</summary>

| Method | When it is called |
|---|---|
| `OnStepEntered` | Every time the step becomes the current step |
| `OnStepExited` | Every time the wizard leaves the step (including before completion on the last step); returns the mutated state |
| `CanAdvance` | Read when the step is entered, whenever it raises `PropertyChanged`, and by `GoNextAsync` |
| `OnDispose` | Called when the host's view factory is disposed |

</details>

---

See also: [NavigationPage](navigation-page.md) · [TabbedPage](tabbed-page.md) · [FlyoutPage](flyout-page.md) · [Popups](popups.md)
