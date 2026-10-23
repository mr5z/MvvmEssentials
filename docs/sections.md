# Sections

A section is a bindable part of a page: one area's state and commands, exposed as a property of the
page's view model and bound by that area of the page. It splits a large page view model into smaller
classes that can be tested and reused separately. See the [main README](../README.md) for setup.

A section derives from `SectionViewModel`, which provides `INotifyPropertyChanged` (the same base as
every page view model) and a dispose hook, and is registered with `RegisterSection`. See
[MapPage vs RegisterPage vs MapView vs RegisterSection](../README.md#mappage-vs-registerpage-vs-mapview-vs-registersection)
for how it compares to the other registrations.

**1. Define the section**

```cs
public sealed partial class RestBarSection(TimeProvider clock) : SectionViewModel
{
    private ITimer? _timer;

    public bool IsResting { get; private set; }

    public string Remaining { get; private set; } = string.Empty;

    public void Start(TimeSpan length)
    {
        // ...
    }

    [RelayCommand]
    private void Skip() => Stop();

    protected override void OnDispose() => _timer?.Dispose();
}
```

**2. Register it**

Each page has its own DI scope, disposed with the page. `RegisterSection` registers the section as
scoped, so it is created with the page's view model and disposed with it; release timers and
subscriptions in `OnDispose`. Don't register a section in the container yourself: a singleton is
never disposed, and a transient gives each injection site its own instance.

```cs
registry.MapPage<WorkoutPage, WorkoutViewModel>()
    .RegisterSection<RestBarSection>();
```

**3. Expose it from the page's view model**

```cs
public sealed partial class WorkoutViewModel(RestBarSection restBar) : PageViewModel
{
    public RestBarSection RestBar { get; } = restBar;

    protected override void OnPageAppearing() => RestBar.Restore();
}
```

**4. Bind that area of the page to it**

```xaml
<ContentPage x:DataType="local:WorkoutViewModel">
    <Grid>
        <!-- ... -->
        <local:RestBarView BindingContext="{Binding RestBar}" />
    </Grid>
</ContentPage>
```

## Limits

- A section has no page lifecycle of its own. The page's view model calls it explicitly
  (`RestBar.Restore()` when the page appears, `RestBar.Stop()` when it leaves), since each section
  needs something different at those points.
- A section is not a `ContentView` with its own scope. For a view created and disposed independently
  of its page, see the [Wizard](wizard.md) steps and `IContentViewFactory`.

## Loading several sections at once

When a page reloads several sections, split each one into two methods: `LoadAsync` reads the data and
returns it as a record without setting any bound property, and `Show` sets the properties from that
record without awaiting. The page awaits every `LoadAsync` first, then calls every `Show`, so all
sections update in the same frame.

```cs
public sealed class GoalCardSection : SectionViewModel
{
    public string Headline { get; private set; } = string.Empty;

    public async Task<Part> LoadAsync(Guid userId)
    {
        var goal = await _goals.GetAsync(userId);
        return new Part(goal);
    }

    public void Show(Part part)
    {
        Headline = part.Goal.Name;
    }

    public sealed record Part(Goal Goal);
}
```
