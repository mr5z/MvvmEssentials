using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
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
    public string? Name { get; set; }
    public int Age { get; set; }
    public int? NullableAge { get; set; }
    public string ReadOnlyProp { get; } = "fixed";
}

// ---------------------------------------------------------------------------
// Page + ViewModel pair for full parameter-mapping pipeline tests.
// MappableViewModel tracks OnParametersSet and exposes mapped properties.
// ---------------------------------------------------------------------------

internal class MappablePage : Page { }

internal class MappableViewModel : PageViewModel
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public int? NullableAge { get; set; }
    public int OnParametersSetCount { get; private set; }
    public INavigationParameters? LastParameters { get; private set; }

    protected override void OnParametersSet(INavigationParameters parameters)
    {
        OnParametersSetCount++;
        LastParameters = parameters;
    }
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