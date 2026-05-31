using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class PageViewModelTests
{
    private TrackablePageViewModel _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new TrackablePageViewModel();

    private void TriggerAppearing() => ((IAppearingAware)_sut).OnPageAppearing();
    private void TriggerDisappearing() => ((IAppearingAware)_sut).OnPageDisappearing();
    private Task TriggerAppearingAsync() => ((IAppearingAwareAsync)_sut).OnPageAppearingAsync();
    private void TriggerNavigatedTo() => ((INavigatedAware)_sut).OnNavigatedTo();
    private void TriggerNavigatedFrom() => ((INavigatedAware)_sut).OnNavigatedFrom();
    private void TriggerPageUnloaded() => ((IPageLoadAware)_sut).OnPageUnloaded();
    private void TriggerDispose() => ((IDisposable)_sut).Dispose();

    private void TriggerNavigatedToRoot(INavigationParameters? p = null)
        => ((IRootPageAware)_sut).OnNavigatedToRoot(p ?? new NavigationParameters());

    private Task TriggerNavigatedToRootAsync(INavigationParameters? p = null)
        => ((IRootPageAwareAsync)_sut).OnNavigatedToRootAsync(p ?? new NavigationParameters());

    // -----------------------------------------------------------------------
    // OnInitialized — called exactly once
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_FirstPageAppearing_IsCalledOnce()
    {
        // When
        TriggerAppearing();

        // Then
        Assert.That(_sut.InitializedCount, Is.EqualTo(1));
    }

    [Test]
    public void OnInitialized_SubsequentAppearings_IsNotCalledAgain()
    {
        // When
        TriggerAppearing();
        TriggerAppearing();
        TriggerAppearing();

        // Then
        Assert.That(_sut.InitializedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnInitializedAsync — called exactly once
    // -----------------------------------------------------------------------

    [Test]
    public async Task OnInitializedAsync_FirstPageAppearingAsync_IsCalledOnce()
    {
        // When
        await TriggerAppearingAsync();

        // Then
        Assert.That(_sut.InitializedAsyncCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnInitializedAsync_SubsequentAppearingsAsync_IsNotCalledAgain()
    {
        // When
        await TriggerAppearingAsync();
        await TriggerAppearingAsync();

        // Then
        Assert.That(_sut.InitializedAsyncCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnPageAppearing — called every time
    // -----------------------------------------------------------------------

    [Test]
    public void OnPageAppearing_CalledMultipleTimes_FiresEachTime()
    {
        // When
        TriggerAppearing();
        TriggerAppearing();
        TriggerAppearing();

        // Then
        Assert.That(_sut.AppearingCount, Is.EqualTo(3));
    }

    // -----------------------------------------------------------------------
    // OnPageDisappearing
    // -----------------------------------------------------------------------

    [Test]
    public void OnPageDisappearing_WhenTriggered_FiresEachTime()
    {
        // When
        TriggerDisappearing();
        TriggerDisappearing();

        // Then
        Assert.That(_sut.DisappearingCount, Is.EqualTo(2));
    }

    // -----------------------------------------------------------------------
    // OnNavigatedTo / OnNavigatedFrom
    // -----------------------------------------------------------------------

    [Test]
    public void OnNavigatedTo_WhenTriggered_Fires()
    {
        // When
        TriggerNavigatedTo();

        // Then
        Assert.That(_sut.NavigatedToCount, Is.EqualTo(1));
    }

    [Test]
    public void OnNavigatedFrom_WhenTriggered_Fires()
    {
        // When
        TriggerNavigatedFrom();

        // Then
        Assert.That(_sut.NavigatedFromCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnPageUnloaded
    // -----------------------------------------------------------------------

    [Test]
    public void OnPageUnloaded_WhenTriggered_Fires()
    {
        // When
        TriggerPageUnloaded();

        // Then
        Assert.That(_sut.PageUnloadedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnDispose
    // -----------------------------------------------------------------------

    [Test]
    public void Dispose_WhenCalled_DelegatesToOnDispose()
    {
        // When
        TriggerDispose();

        // Then
        Assert.That(_sut.DisposeCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnNavigatedToRoot (sync and async are separate paths)
    // -----------------------------------------------------------------------

    [Test]
    public void OnNavigatedToRoot_WhenTriggered_Fires()
    {
        // When
        TriggerNavigatedToRoot();

        // Then
        Assert.That(_sut.NavigatedToRootCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnNavigatedToRootAsync_WhenTriggered_Fires()
    {
        // When
        await TriggerNavigatedToRootAsync();

        // Then
        Assert.That(_sut.NavigatedToRootAsyncCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // Initialization is independent across instances
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_TwoSeparateInstances_EachInitializeOnce()
    {
        // Given
        var second = new TrackablePageViewModel();

        // When
        TriggerAppearing();
        ((IAppearingAware)second).OnPageAppearing();

        // Then
        Assert.That(_sut.InitializedCount, Is.EqualTo(1));
        Assert.That(second.InitializedCount, Is.EqualTo(1));
    }
}
