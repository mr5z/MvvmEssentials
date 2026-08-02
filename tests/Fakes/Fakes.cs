using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Attributes;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.Services.TabbedPages;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.UnitTest.Fakes;

// ---------------------------------------------------------------------------
// Minimal Page / ViewModel stubs for registry and factory tests
// ---------------------------------------------------------------------------

internal class FakePage : Page { }
internal class FakeSecondPage : Page { }
internal class FakeViewModel : PageViewModel { }
internal class FakeSecondViewModel : PageViewModel { }

// ---------------------------------------------------------------------------
// Trackable PageViewModel — records every lifecycle call
// ---------------------------------------------------------------------------

internal class TrackablePageViewModel : PageViewModel
{
    public int InitializedCount { get; private set; }
    public int InitializedAsyncCount { get; private set; }
    public int AppearingCount { get; private set; }
    public int DisappearingCount { get; private set; }
    public int NavigatedToCount { get; private set; }
    public int NavigatedFromCount { get; private set; }
    public int PageUnloadedCount { get; private set; }
    public int DisposeCount { get; private set; }
    public int NavigatedToRootCount { get; private set; }
    public int NavigatedToRootAsyncCount { get; private set; }
    public INavigationParameters? LastParameters { get; private set; }

    protected override void OnInitialized() => InitializedCount++;

    protected override Task OnInitializedAsync()
    {
        InitializedAsyncCount++;
        return Task.CompletedTask;
    }

    protected override void OnPageAppearing()
    {
        base.OnPageAppearing(); // triggers OnInitialized guard
        AppearingCount++;
    }

    protected override void OnPageDisappearing() => DisappearingCount++;
    protected override void OnNavigatedTo() => NavigatedToCount++;
    protected override void OnNavigatedFrom() => NavigatedFromCount++;
    protected override void OnPageUnloaded() => PageUnloadedCount++;
    protected override void OnDispose() => DisposeCount++;

    protected override void OnParametersSet(INavigationParameters parameters)
        => LastParameters = parameters;

    protected override void OnNavigatedToRoot(INavigationParameters parameters)
        => NavigatedToRootCount++;

    protected override Task OnNavigatedToRootAsync(INavigationParameters parameters)
    {
        NavigatedToRootAsyncCount++;
        return Task.CompletedTask;
    }
}

// ---------------------------------------------------------------------------
// Trackable TabViewModel
// ---------------------------------------------------------------------------

internal class TrackableTabViewModel : TabViewModel
{
    public int InitializedCount { get; private set; }
    public int SelectedCount { get; private set; }
    public int SelectedAsyncCount { get; private set; }
    public int UnselectedCount { get; private set; }
    public int UnselectedAsyncCount { get; private set; }
    public int DisposeCount { get; private set; }

    protected override void OnInitialized() => InitializedCount++;

    protected override void OnTabSelected()
    {
        base.OnTabSelected(); // triggers OnInitialized guard
        SelectedCount++;
    }

    protected override async Task OnTabSelectedAsync()
    {
        await base.OnTabSelectedAsync(); // triggers OnInitializedAsync guard
        SelectedAsyncCount++;
    }

    protected override void OnTabUnselected() => UnselectedCount++;
    protected override Task OnTabUnselectedAsync() { UnselectedAsyncCount++; return Task.CompletedTask; }
    protected override void OnDispose() => DisposeCount++;
}

// ---------------------------------------------------------------------------
// Trackable FlyoutViewModel
// ---------------------------------------------------------------------------

internal class TrackableFlyoutMenuViewModel : FlyoutMenuViewModel
{
    public int OpenedCount { get; private set; }
    public int OpenedAsyncCount { get; private set; }
    public int ClosedCount { get; private set; }
    public int ClosedAsyncCount { get; private set; }
    public int DisposeCount { get; private set; }

    protected override void OnFlyoutOpened() => OpenedCount++;
    protected override Task OnFlyoutOpenedAsync() { OpenedAsyncCount++; return Task.CompletedTask; }
    protected override void OnFlyoutClosed() => ClosedCount++;
    protected override Task OnFlyoutClosedAsync() { ClosedAsyncCount++; return Task.CompletedTask; }
    protected override void OnDispose() => DisposeCount++;
}

// ---------------------------------------------------------------------------
// Concrete PopupViewModel for testing protected members
// ---------------------------------------------------------------------------

internal record TestPopupResult(bool Confirmed);

internal partial class TestPopupViewModel(IPopupService popupService)
    : PopupViewModel<TestPopupResult>(popupService)
{
    public Task<IResult> PublicDismissWithResult(TestPopupResult result) => Dismiss(result);
    public bool PublicShouldDismissOnBackButtonPressed => ShouldDismissOnBackButtonPressed;
    public bool PublicShouldDismissOnBackgroundTapped => ShouldDismissOnBackgroundTapped;
}

// ---------------------------------------------------------------------------
// NavigableEntryViewModel with public properties for parameter-mapping tests
// ---------------------------------------------------------------------------

internal class PropertiedViewModel : NavigableEntryViewModel
{
    [NavigationParameter]
    public string? Name { get; set; }
    
    [NavigationParameter]
    public int Age { get; set; }
    
    [NavigationParameter]
    public int? NullableAge { get; set; }
    
    public string ReadOnlyProp { get; } = "fixed";
}

// ---------------------------------------------------------------------------
// Page + ViewModel pair for full parameter-mapping pipeline tests.
// MappableViewModel tracks OnParametersSet and exposes mapped properties.
// ---------------------------------------------------------------------------

internal class MappablePage : Page { }
internal class MappableSecondPage : Page { }

internal class MappableViewModel : PageViewModel
{
    public int DisposeCount { get; private set; }
    
    [NavigationParameter]
    public string? Name { get; set; }
    
    [NavigationParameter]
    public int Age { get; set; }
    
    [NavigationParameter]
    public int? NullableAge { get; set; }
    
    [NavigationParameter]
    public bool IsEditing { get; set; }

    [NavigationParameter]
    public Guid CorrelationId { get; set; }
    
    public string? UnmappedName { get; set; }
    public int UnmappedAge { get; set; }
    public string? UnmappedPrivateSet { get; private set; }
    
    public int OnParametersSetCount { get; private set; }
    public INavigationParameters? LastParameters { get; private set; }

    protected override void OnParametersSet(INavigationParameters parameters)
    {
        OnParametersSetCount++;
        LastParameters = parameters;
    }
    
    protected override void OnDispose() => DisposeCount++;
}

// ---------------------------------------------------------------------------
// TabHostViewModel stub that exposes a single controllable tab
// ---------------------------------------------------------------------------

internal class SingleTabHostViewModel : TabHostViewModel
{
    private readonly TrackableTabViewModel _tab;

    public SingleTabHostViewModel(TrackableTabViewModel tab) => _tab = tab;

    protected override IReadOnlyCollection<ITabComponent> Tabs => [_tab];
}

internal sealed class DisposalTracker
{
    public List<LeakProbeViewModel> Created { get; } = [];
}

internal class LeakProbePage : Page { }
internal class LeakProbeSecondPage : Page { }

internal class LeakProbeViewModel : PageViewModel
{
    public int DisposeCount { get; private set; }

    public LeakProbeViewModel(DisposalTracker tracker) => tracker.Created.Add(this);

    protected override void OnDispose() => DisposeCount++;
}

internal class TestDispatcherProvider : IDispatcherProvider
{
    public IDispatcher GetForCurrentThread() => new TestDispatcher();
}

internal class TestDispatcher : IDispatcher
{
    public bool IsDispatchRequired => false;
    public bool Dispatch(Action action) { action(); return true; }
    public bool DispatchDelayed(TimeSpan delay, Action action) { action(); return true; }
    public IDispatcherTimer CreateTimer() => throw new NotImplementedException();
}

internal class TrackableRootPageNavigatedViewModel : IRootPageNavigated
{
    public int OnNavigatedToRootCount { get; private set; }
    public int OnNavigatedToRootAsyncCount { get; private set; }
    public INavigationParameters? LastParameters { get; private set; }
    public bool ThrowOnAsync { get; set; }

    void IRootPageNavigated.OnNavigatedToRoot(INavigationParameters parameters)
    {
        OnNavigatedToRootCount++;
        LastParameters = parameters;
    }

    Task IRootPageNavigated.OnNavigatedToRootAsync(INavigationParameters parameters)
    {
        OnNavigatedToRootAsyncCount++;
        if (ThrowOnAsync)
            throw new InvalidOperationException("Simulated failure.");
        return Task.CompletedTask;
    }
}

internal sealed class TestFlyoutViewModel(TrackableFlyoutMenuViewModel menu, TrackableFlyoutMenuViewModel detail)
    : FlyoutViewModel<TrackableFlyoutMenuViewModel, TrackableFlyoutMenuViewModel>(menu, detail);