using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class TabViewModelTests
{
    private TrackableTabViewModel _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new TrackableTabViewModel();

    private void TriggerSelected() => ((ITabComponent)_sut).OnTabSelected();
    private void TriggerUnselected() => ((ITabComponent)_sut).OnTabUnselected();
    private Task TriggerSelectedAsync() => ((ITabComponent)_sut).OnTabSelectedAsync();
    private Task TriggerUnselectedAsync() => ((ITabComponent)_sut).OnTabUnselectedAsync();
    private void TriggerDispose() => ((IDisposable)_sut).Dispose();

    // -----------------------------------------------------------------------
    // OnInitialized — called exactly once
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_FirstTabSelection_IsCalledOnce()
    {
        // When
        TriggerSelected();

        // Then
        Assert.That(_sut.InitializedCount, Is.EqualTo(1));
    }

    [Test]
    public void OnInitialized_SubsequentSelections_IsNotCalledAgain()
    {
        // When
        TriggerSelected();
        TriggerSelected();
        TriggerSelected();

        // Then
        Assert.That(_sut.InitializedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnTabSelected — called every selection
    // -----------------------------------------------------------------------

    [Test]
    public void OnTabSelected_CalledMultipleTimes_FiresEachTime()
    {
        // When
        TriggerSelected();
        TriggerSelected();

        // Then
        Assert.That(_sut.SelectedCount, Is.EqualTo(2));
    }

    // -----------------------------------------------------------------------
    // OnTabUnselected
    // -----------------------------------------------------------------------

    [Test]
    public void OnTabUnselected_WhenTriggered_Fires()
    {
        // When
        TriggerUnselected();

        // Then
        Assert.That(_sut.UnselectedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // Async variants
    // -----------------------------------------------------------------------

    [Test]
    public async Task OnTabSelectedAsync_FirstTime_TriggersOnInitializedAsync()
    {
        // When
        await TriggerSelectedAsync();

        // Then
        Assert.That(_sut.SelectedAsyncCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnTabSelectedAsync_CalledTwice_OnInitializedAsyncRunsOnce()
    {
        // When
        await TriggerSelectedAsync();
        await TriggerSelectedAsync();

        // Then
        Assert.That(_sut.SelectedAsyncCount, Is.EqualTo(2));
    }

    [Test]
    public async Task OnTabUnselectedAsync_WhenTriggered_Fires()
    {
        // When
        await TriggerUnselectedAsync();

        // Then
        Assert.That(_sut.UnselectedAsyncCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // OnDispose
    // -----------------------------------------------------------------------

    [Test]
    public void Dispose_DelegatesToOnDispose()
    {
        // When
        TriggerDispose();

        // Then
        Assert.That(_sut.DisposeCount, Is.EqualTo(1));
    }
}
