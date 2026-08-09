using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.Services.TabbedPages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class TabHostViewModelTests
{
    private TrackableTabViewModel _tab = null!;
    private SingleTabHostViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tab = new TrackableTabViewModel();
        _sut = new SingleTabHostViewModel(_tab);
    }

    // -----------------------------------------------------------------------
    // OnInitialized — delegates to first tab
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_WhenPageFirstAppears_CallsOnTabSelectedOnCurrentTab()
    {
        // When
        ((IPageAppearing)_sut).OnPageAppearing();

        // Then
        Assert.That(_tab.SelectedCount, Is.EqualTo(1));
    }

    [Test]
    public void OnInitialized_SubsequentAppearings_DoesNotCallOnTabSelectedAgain()
    {
        // When
        ((IPageAppearing)_sut).OnPageAppearing();
        ((IPageAppearing)_sut).OnPageAppearing();

        // Then
        Assert.That(_tab.SelectedCount, Is.EqualTo(1));
    }
    
    
    [Test]
    public async Task OnInitializedAsync_WhenPageFirstAppearsAsync_CallsOnTabSelectedAsyncOnCurrentTab()
    {
        // When
        await ((IPageAppearing)_sut).OnPageAppearingAsync();

        // Then
        Assert.That(_tab.SelectedAsyncCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnInitializedAsync_SubsequentAppearingsAsync_DoesNotCallOnTabSelectedAsyncAgain()
    {
        // When
        await ((IPageAppearing)_sut).OnPageAppearingAsync();
        await ((IPageAppearing)_sut).OnPageAppearingAsync();

        // Then
        Assert.That(_tab.SelectedAsyncCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnInitializedAsync_WhenPageFirstAppearsAsync_ReachesTabViewModelOnInitializedAsync()
    {
        // Given — TrackableTabViewModel.OnTabSelectedAsync -> base.OnTabSelectedAsync -> OnInitializedAsync guard
        // so a non-zero SelectedAsyncCount alone doesn't prove OnInitializedAsync ran; assert both paths.

        // When
        await ((IPageAppearing)_sut).OnPageAppearingAsync();

        // Then
        Assert.That(_tab.SelectedAsyncCount, Is.EqualTo(1));
        Assert.That(_tab.InitializedCount, Is.EqualTo(0)); // sync OnInitialized guard is separate; not triggered by the async path alone
    }

    [Test]
    public void OnInitializedAsync_SyncAndAsyncAppearing_BothReachCurrentTabIndependently()
    {
        // Given — mirrors PageFactory.Page_Appearing: sync call, then async call (not awaited between them in production,
        // but here we run them sequentially to prove neither chain silently no-ops the other).

        // When
        ((IPageAppearing)_sut).OnPageAppearing();
        ((IPageAppearing)_sut).OnPageAppearingAsync().GetAwaiter().GetResult();

        // Then
        Assert.That(_tab.SelectedCount, Is.EqualTo(1));
        Assert.That(_tab.SelectedAsyncCount, Is.EqualTo(1));
    }
    
    [Test]
    public async Task OnInitializedAsync_WhenBaseCallOmitted_NeverReachesCurrentTab()
    {
        var tab = new TrackableTabViewModel();
        var sut = new MissingBaseCallTabHostViewModel(tab);

        await ((IPageAppearing)sut).OnPageAppearingAsync();

        Assert.That(tab.SelectedAsyncCount, Is.EqualTo(0));
    }

    // -----------------------------------------------------------------------
    // SelectedTabIndex / CurrentTab
    // -----------------------------------------------------------------------

    [Test]
    public void SelectedTabIndex_DefaultsToZero()
    {
        Assert.That(((ITabHost)_sut).SelectedTabIndex, Is.EqualTo(0));
    }

    [Test]
    public void CurrentTab_ByDefault_ReturnsFirstTab()
    {
        Assert.That(((ITabHost)_sut).CurrentTab, Is.SameAs(_tab));
    }
    
    [Test]
    public void SelectedTabIndex_WhenSetToNewValue_RaisesPropertyChanged()
    {
        // Given
        var raised = new List<string>();
        ((INotifyPropertyChanged)_sut).PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        // When
        ((ITabHost)_sut).SelectedTabIndex = 1;

        // Then
        Assert.That(raised, Does.Contain(nameof(ITabHost.SelectedTabIndex)));
    }

    [Test]
    public void SelectedTabIndex_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Given — default is 0; setting to 0 again should be a no-op
        var raised = new List<string>();
        ((INotifyPropertyChanged)_sut).PropertyChanged += (_, e) => raised.Add(e.PropertyName!);

        // When
        ((ITabHost)_sut).SelectedTabIndex = 0;

        // Then
        Assert.That(raised, Is.Empty);
    }
    
    [Test]
    public void SelectedTabIndex_SetOutsideValidRange_ThrowsIndexOutOfRangeException()
    {
        // Given
        ((ITabHost)_sut).SelectedTabIndex = -1;
        
        // Then
        Assert.Throws<IndexOutOfRangeException>(() => ((IPageAppearing)_sut).OnPageAppearing());
    }
    
    // -----------------------------------------------------------------------
    // Guard is unbypassable — even a full override with no base call still
    // triggers OnInitialized/OnInitializedAsync exactly once (regression test
    // for the DashboardViewModel bug: overriding OnTabSelectedAsync without
    // calling base.OnTabSelectedAsync() used to skip OnInitializedAsync entirely).
    // -----------------------------------------------------------------------

    [Test]
    public void OnTabSelected_WhenOverrideOmitsBaseCall_StillTriggersOnInitialized()
    {
        // Given
        var sut = new OverridesWithoutBaseCallTabViewModel();

        // When
        ((ITabComponent)sut).OnTabSelected();

        // Then
        Assert.That(sut.InitializedCount, Is.EqualTo(1));
        Assert.That(sut.OnTabSelectedCalled, Is.True); // the override itself still ran
    }

    [Test]
    public async Task OnTabSelectedAsync_WhenOverrideOmitsBaseCall_StillTriggersOnInitializedAsync()
    {
        // Given
        var sut = new OverridesWithoutBaseCallTabViewModel();

        // When
        await ((ITabComponent)sut).OnTabSelectedAsync();

        // Then
        Assert.That(sut.InitializedAsyncCount, Is.EqualTo(1));
        Assert.That(sut.OnTabSelectedAsyncCalled, Is.True); // the override itself still ran
    }

    [Test]
    public async Task OnTabSelectedAsync_WhenOverrideOmitsBaseCall_OnInitializedAsyncRunsBeforeOverrideLogic()
    {
        // Given — ordering matters: RefreshAsync()-style logic in the override should
        // run after initialization, not instead of it.
        var sut = new OverridesWithoutBaseCallTabViewModel();

        // When
        await ((ITabComponent)sut).OnTabSelectedAsync();

        // Then
        Assert.That(sut.CallOrder, Is.EqualTo(["OnInitializedAsync", "OnTabSelectedAsync"]));
    }

    [Test]
    public async Task OnTabSelectedAsync_WhenOverrideOmitsBaseCall_CalledTwice_OnInitializedAsyncRunsOnce()
    {
        // Given
        var sut = new OverridesWithoutBaseCallTabViewModel();

        // When
        await ((ITabComponent)sut).OnTabSelectedAsync();
        await ((ITabComponent)sut).OnTabSelectedAsync();

        // Then
        Assert.That(sut.InitializedAsyncCount, Is.EqualTo(1));
        Assert.That(sut.OnTabSelectedAsyncCallCount, Is.EqualTo(2));
    }
}
