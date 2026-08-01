using System.ComponentModel;
using Nkraft.MvvmEssentials.Behaviors;
using Nkraft.MvvmEssentials.Services.FlyoutPages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Behaviors;

// IFlyoutHost is internal; a minimal INotifyPropertyChanged-backed test double lets us
// drive both the native→ViewModel and ViewModel→native sync branches.
internal sealed class FakeFlyoutHost(IFlyoutComponent menu, IFlyoutComponent detail)
    : IFlyoutHost, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsPresented
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPresented)));
        }
    }

    public IFlyoutComponent MenuViewModel { get; } = menu;

    public IFlyoutComponent DetailViewModel { get; } = detail;
}

[TestFixture]
public class FlyoutPresentingBehaviorTests
{
    private FlyoutPresentingBehavior _sut = null!;
    private TrackableFlyoutMenuViewModel _menu = null!;
    private TrackableFlyoutMenuViewModel _detail = null!;
    private FakeFlyoutHost _host = null!;
    private FlyoutPage _flyoutPage = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new FlyoutPresentingBehavior();
        _menu = new TrackableFlyoutMenuViewModel();
        _detail = new TrackableFlyoutMenuViewModel();
        _host = new FakeFlyoutHost(_menu, _detail);
        _flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new ContentPage()
        };
    }

    // -----------------------------------------------------------------------
    // Native IsPresented change → ViewModel + component callbacks
    // (BindingContext set before attach is fine here — FlyoutPage_IsPresentedChanged
    // and SetupFlyoutHost both read BindingContext live, not via a subscription.)
    // -----------------------------------------------------------------------

    [Test]
    public void IsPresentedChanged_WhenOpenedNatively_UpdatesHostIsPresented()
    {
        // Given
        _flyoutPage.BindingContext = _host;
        _flyoutPage.Behaviors.Add(_sut);

        // When
        _flyoutPage.IsPresented = true;

        // Then
        Assert.That(_host.IsPresented, Is.True);
    }

    [Test]
    public void IsPresentedChanged_WhenOpenedNatively_FiresOnFlyoutOpenedOnBothComponents()
    {
        // Given
        _flyoutPage.BindingContext = _host;
        _flyoutPage.Behaviors.Add(_sut);

        // When
        _flyoutPage.IsPresented = true;

        // Then
        Assert.That(_menu.OpenedCount, Is.EqualTo(1));
        Assert.That(_detail.OpenedCount, Is.EqualTo(1));
    }

    [Test]
    public void IsPresentedChanged_WhenClosedNatively_FiresOnFlyoutClosedOnBothComponents()
    {
        // Given
        _flyoutPage.BindingContext = _host;
        _flyoutPage.IsPresented = true;
        _flyoutPage.Behaviors.Add(_sut);

        // When
        _flyoutPage.IsPresented = false;

        // Then
        Assert.That(_menu.ClosedCount, Is.EqualTo(1));
        Assert.That(_detail.ClosedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // ViewModel-driven IsPresented change → native sync
    // The PropertyChanged subscription is wired up inside the BindingContextChanged
    // handler, so BindingContext must be assigned AFTER the behavior attaches.
    // -----------------------------------------------------------------------

    [Test]
    public void ViewModelIsPresentedChanged_WhenSetDirectly_UpdatesNativeFlyoutPage()
    {
        // Given
        _flyoutPage.Behaviors.Add(_sut);
        _flyoutPage.BindingContext = _host;

        // When
        _host.IsPresented = true;

        // Then
        Assert.That(_flyoutPage.IsPresented, Is.True);
    }

    // -----------------------------------------------------------------------
    // Detach — stops responding to native changes
    // -----------------------------------------------------------------------

    [Test]
    public void Detach_StopsSyncingNativeIsPresentedToViewModel()
    {
        // Given
        _flyoutPage.BindingContext = _host;
        _flyoutPage.Behaviors.Add(_sut);
        _flyoutPage.Behaviors.Remove(_sut);

        // When
        _flyoutPage.IsPresented = true;

        // Then
        Assert.That(_host.IsPresented, Is.False);
    }
}