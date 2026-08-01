using System.ComponentModel;
using Nkraft.MvvmEssentials.Behaviors;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.TabbedPages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Behaviors;

// ITabHost is internal; a minimal INotifyPropertyChanged-backed test double lets us
// drive both the native→ViewModel and ViewModel→native sync branches.
internal sealed class FakeTabHost(IReadOnlyCollection<ITabComponent> tabs) : ITabHost, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int SelectedTabIndex
    {
        get;
        set
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTabIndex)));
        }
    }

    public ITabComponent CurrentTab => Tabs.ElementAt(SelectedTabIndex);

    public IReadOnlyCollection<ITabComponent> Tabs { get; } = tabs;
}

[TestFixture]
public class TabSelectionBehaviorTests
{
    private TabSelectionBehavior _sut = null!;
    private TrackableTabViewModel _tab1 = null!;
    private TrackableTabViewModel _tab2 = null!;
    private FakeTabHost _host = null!;
    private Page _page1 = null!;
    private Page _page2 = null!;
    private TabbedPage _tabbedPage = null!;

    [SetUp]
    public void SetUp()
    {
        DispatcherProvider.SetCurrent(new TestDispatcherProvider());
        _sut = new TabSelectionBehavior();
        _tab1 = new TrackableTabViewModel();
        _tab2 = new TrackableTabViewModel();
        _host = new FakeTabHost([_tab1, _tab2]);
        _page1 = new Page();
        _page2 = new Page();
        _tabbedPage = new TabbedPage();
        _tabbedPage.Children.Add(_page1);
        _tabbedPage.Children.Add(_page2);
    }

    // -----------------------------------------------------------------------
    // Native CurrentPage change → ViewModel + tab lifecycle
    // -----------------------------------------------------------------------

    [Test]
    public void CurrentPageChanged_FirstSelection_SelectsOnlyTheCurrentTab()
    {
        // Given
        _tabbedPage.BindingContext = _host;
        _tabbedPage.Behaviors.Add(_sut);

        // When — first real change away from the auto-selected page1
        _tabbedPage.CurrentPage = _page2;

        // Then
        Assert.That(_tab2.SelectedCount, Is.EqualTo(1));
        Assert.That(_tab1.UnselectedCount, Is.EqualTo(0));
    }

    [Test]
    public void CurrentPageChanged_FirstSelection_UpdatesHostSelectedTabIndex()
    {
        // Given
        _tabbedPage.BindingContext = _host;
        _tabbedPage.Behaviors.Add(_sut);

        // When
        _tabbedPage.CurrentPage = _page2;

        // Then
        Assert.That(_host.SelectedTabIndex, Is.EqualTo(1));
    }

    [Test]
    public void CurrentPageChanged_SubsequentSelection_UnselectsThePreviousTab()
    {
        // Given
        _tabbedPage.BindingContext = _host;
        _tabbedPage.Behaviors.Add(_sut);
        _tabbedPage.CurrentPage = _page2;

        // When — switch back to the first tab
        _tabbedPage.CurrentPage = _page1;

        // Then
        Assert.That(_tab2.UnselectedCount, Is.EqualTo(1));
        Assert.That(_tab1.SelectedCount, Is.EqualTo(1));
        Assert.That(_host.SelectedTabIndex, Is.EqualTo(0));
    }

    // -----------------------------------------------------------------------
    // ViewModel-driven SelectedTabIndex → native CurrentPage sync
    // The PropertyChanged subscription is wired up inside BindingContextChanged,
    // so BindingContext must be assigned AFTER the behavior attaches.
    // -----------------------------------------------------------------------

    [Test]
    public void SelectedTabIndexChanged_WhenSetDirectly_UpdatesNativeCurrentPage()
    {
        // Given
        _tabbedPage.Behaviors.Add(_sut);
        _tabbedPage.BindingContext = _host;

        // When
        _host.SelectedTabIndex = 1;

        // Then
        Assert.That(_tabbedPage.CurrentPage, Is.SameAs(_page2));
    }

    [Test]
    public void SelectedTabIndexChanged_WithOutOfRangeIndex_DoesNotThrowOrChangeCurrentPage()
    {
        // Given
        _tabbedPage.Behaviors.Add(_sut);
        _tabbedPage.BindingContext = _host;

        // When
        Assert.DoesNotThrow(() => _host.SelectedTabIndex = 99);

        // Then
        Assert.That(_tabbedPage.CurrentPage, Is.SameAs(_page1));
    }

    // -----------------------------------------------------------------------
    // Detach — stops responding to native changes
    // -----------------------------------------------------------------------

    [Test]
    public void Detach_StopsRespondingToCurrentPageChanges()
    {
        // Given
        _tabbedPage.BindingContext = _host;
        _tabbedPage.Behaviors.Add(_sut);
        _tabbedPage.Behaviors.Remove(_sut);

        // When
        _tabbedPage.CurrentPage = _page2;

        // Then
        Assert.That(_tab2.SelectedCount, Is.EqualTo(0));
    }
}