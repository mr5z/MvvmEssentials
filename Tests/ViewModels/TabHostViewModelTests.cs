using Nkraft.MvvmEssentials.Services.Navigation;
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
        ((IAppearingAware)_sut).OnPageAppearing();

        // Then
        Assert.That(_tab.SelectedCount, Is.EqualTo(1));
    }

    [Test]
    public void OnInitialized_SubsequentAppearings_DoesNotCallOnTabSelectedAgain()
    {
        // When
        ((IAppearingAware)_sut).OnPageAppearing();
        ((IAppearingAware)_sut).OnPageAppearing();

        // Then
        Assert.That(_tab.SelectedCount, Is.EqualTo(1));
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
}
