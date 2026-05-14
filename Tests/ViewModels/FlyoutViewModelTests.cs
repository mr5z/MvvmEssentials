using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class FlyoutViewModelTests
{
    private TrackableFlyoutViewModel _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new TrackableFlyoutViewModel();

    private void TriggerOpened() => ((IFlyoutComponent)_sut).OnFlyoutOpened();
    private void TriggerClosed() => ((IFlyoutComponent)_sut).OnFlyoutClosed();
    private Task TriggerOpenedAsync() => ((IFlyoutComponent)_sut).OnFlyoutOpenedAsync();
    private Task TriggerClosedAsync() => ((IFlyoutComponent)_sut).OnFlyoutClosedAsync();
    private void TriggerDispose() => ((IDisposable)_sut).Dispose();

    // -----------------------------------------------------------------------
    // Sync lifecycle
    // -----------------------------------------------------------------------

    [Test]
    public void OnFlyoutOpened_WhenTriggered_FiresEachTime()
    {
        // When
        TriggerOpened();
        TriggerOpened();

        // Then
        Assert.That(_sut.OpenedCount, Is.EqualTo(2));
    }

    [Test]
    public void OnFlyoutClosed_WhenTriggered_FiresEachTime()
    {
        // When
        TriggerClosed();
        TriggerClosed();

        // Then
        Assert.That(_sut.ClosedCount, Is.EqualTo(2));
    }

    // -----------------------------------------------------------------------
    // Async lifecycle
    // -----------------------------------------------------------------------

    [Test]
    public async Task OnFlyoutOpenedAsync_WhenTriggered_Fires()
    {
        // When
        await TriggerOpenedAsync();

        // Then
        Assert.That(_sut.OpenedAsyncCount, Is.EqualTo(1));
    }

    [Test]
    public async Task OnFlyoutClosedAsync_WhenTriggered_Fires()
    {
        // When
        await TriggerClosedAsync();

        // Then
        Assert.That(_sut.ClosedAsyncCount, Is.EqualTo(1));
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

    // -----------------------------------------------------------------------
    // Open/close symmetry
    // -----------------------------------------------------------------------

    [Test]
    public async Task OpenedAndClosed_Sequence_CountsMatchInvocations()
    {
        // Given — simulate open → close → open → close
        TriggerOpened();
        TriggerClosed();
        await TriggerOpenedAsync();
        await TriggerClosedAsync();

        // Then
        Assert.That(_sut.OpenedCount, Is.EqualTo(1));
        Assert.That(_sut.ClosedCount, Is.EqualTo(1));
        Assert.That(_sut.OpenedAsyncCount, Is.EqualTo(1));
        Assert.That(_sut.ClosedAsyncCount, Is.EqualTo(1));
    }
}
