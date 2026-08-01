using Nkraft.MvvmEssentials.Behaviors;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Behaviors;

[TestFixture]
public class FlyoutDetailLifecycleBehaviorTests
{
    private FlyoutDetailLifecycleBehavior _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new FlyoutDetailLifecycleBehavior();
    }

    // -----------------------------------------------------------------------
    // Attach — initial Detail already set
    // -----------------------------------------------------------------------

    [Test]
    public void Attach_WhenDetailAlreadySet_TriggersNavigatedToOnDetailBindingContext()
    {
        // Given
        var detailVm = new TrackablePageViewModel();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page { BindingContext = detailVm }
        };

        // When
        flyoutPage.Behaviors.Add(_sut);

        // Then
        Assert.That(detailVm.NavigatedToCount, Is.EqualTo(1));
    }

    [Test]
    public void Attach_WhenNoDetailSet_DoesNotThrow()
    {
        // Given
        var flyoutPage = new FlyoutPage { Flyout = new ContentPage { Title = "test" } };

        // Then
        Assert.DoesNotThrow(() => flyoutPage.Behaviors.Add(_sut));
    }

    // -----------------------------------------------------------------------
    // Appearing / Disappearing propagation
    // -----------------------------------------------------------------------

    [Test]
    public void Appearing_WhenDetailBindingContextSupportsIt_PropagatesToDetail()
    {
        // Given
        var detailVm = new TrackablePageViewModel();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page { BindingContext = detailVm }
        };
        flyoutPage.Behaviors.Add(_sut);

        // When — FlyoutPage_Appearing is internal specifically so tests can call it
        // directly; FlyoutPage.SendAppearing() doesn't reliably raise the FlyoutPage's
        // own Appearing event in a headless test host, so we bypass that native path.
        _sut.FlyoutPage_Appearing(flyoutPage, EventArgs.Empty);

        // Then
        Assert.That(detailVm.AppearingCount, Is.EqualTo(1));
    }

    [Test]
    public void Disappearing_WhenDetailBindingContextSupportsIt_PropagatesToDetail()
    {
        // Given
        var detailVm = new TrackablePageViewModel();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page { BindingContext = detailVm }
        };
        flyoutPage.Behaviors.Add(_sut);

        // When
        _sut.FlyoutPage_Disappearing(flyoutPage, EventArgs.Empty);

        // Then
        Assert.That(detailVm.DisappearingCount, Is.EqualTo(1));
    }

    [Test]
    public void Appearing_WhenDetailIsNavigationPage_PropagatesToItsCurrentPage()
    {
        // Given — GetTargetPage unwraps NavigationPage.CurrentPage
        var detailVm = new TrackablePageViewModel();
        var navigationDetail = new NavigationPage(new Page { BindingContext = detailVm });
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = navigationDetail
        };
        flyoutPage.Behaviors.Add(_sut);

        // When
        _sut.FlyoutPage_Appearing(flyoutPage, EventArgs.Empty);

        // Then
        Assert.That(detailVm.AppearingCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // Detail reassignment — triggers navigated-to on the new detail
    // -----------------------------------------------------------------------

    [Test]
    public void DetailChanged_WhenReassigned_TriggersNavigatedToOnNewDetail()
    {
        // Given
        var flyoutPage = new FlyoutPage { Flyout = new ContentPage { Title = "test" } };
        flyoutPage.Behaviors.Add(_sut);

        var newDetailVm = new TrackablePageViewModel();

        // When
        flyoutPage.Detail = new Page { BindingContext = newDetailVm };

        // Then
        Assert.That(newDetailVm.NavigatedToCount, Is.EqualTo(1));
    }

    // Note: the Detail case's `when (flyoutPage.Detail is {} newDetail)` guard's false
    // path (Detail reassigned to null) is untestable and, in practice, unreachable —
    // MAUI's FlyoutPage throws ArgumentNullException if Detail is set to null after
    // already having a value, so that branch can never fire through legitimate usage.

    // -----------------------------------------------------------------------
    // Flyout closing while already presented — re-triggers Appearing on Detail
    // -----------------------------------------------------------------------

    [Test]
    public void IsPresentedChanged_WhenClosingAfterBeingPresented_PropagatesAppearingToDetail()
    {
        // Given — behavior captures _wasPresented at attach time
        var detailVm = new TrackablePageViewModel();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page { BindingContext = detailVm },
            IsPresented = true
        };
        flyoutPage.Behaviors.Add(_sut);

        // When
        flyoutPage.IsPresented = false;

        // Then
        Assert.That(detailVm.AppearingCount, Is.EqualTo(1));
    }

    [Test]
    public void IsPresentedChanged_WhenOpening_DoesNotPropagateAppearingToDetail()
    {
        // Given — behavior only re-propagates Appearing when transitioning from presented to not
        var detailVm = new TrackablePageViewModel();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page { BindingContext = detailVm },
            IsPresented = false
        };
        flyoutPage.Behaviors.Add(_sut);

        // When
        flyoutPage.IsPresented = true;

        // Then
        Assert.That(detailVm.AppearingCount, Is.EqualTo(0));
    }

    [Test]
    public void IsPresentedChanged_WhenClosingWithNoDetailSet_DoesNotThrow()
    {
        // Given — the compound condition's Detail:{} pattern is false when Detail is null
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            IsPresented = true
        };
        flyoutPage.Behaviors.Add(_sut);

        // Then
        Assert.DoesNotThrow(() => flyoutPage.IsPresented = false);
    }

    [Test]
    public void PropertyChanged_ForUnrelatedProperty_DoesNotThrow()
    {
        // Given — neither switch case matches, so this exercises the implicit no-op default
        var flyoutPage = new FlyoutPage { Flyout = new ContentPage { Title = "test" } };
        flyoutPage.Behaviors.Add(_sut);

        // Then
        Assert.DoesNotThrow(() => flyoutPage.Title = "Something else");
    }

    // -----------------------------------------------------------------------
    // Detach — stops responding to native events
    // -----------------------------------------------------------------------

    [Test]
    public void Detach_StopsPropagatingAppearingToDetail()
    {
        // Given
        var detailVm = new TrackablePageViewModel();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page { BindingContext = detailVm }
        };
        flyoutPage.Behaviors.Add(_sut);
        flyoutPage.Behaviors.Remove(_sut);

        // When — FlyoutPage is nulled out in OnDetachingFrom, so the handler becomes
        // a no-op even when invoked directly
        _sut.FlyoutPage_Appearing(flyoutPage, EventArgs.Empty);

        // Then
        Assert.That(detailVm.AppearingCount, Is.EqualTo(0));
    }
}