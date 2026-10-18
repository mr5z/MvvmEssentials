using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.MvvmEssentials.Pages;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

/// <summary>
/// Regression tests: Android raises Unloaded when it destroys a backgrounded Activity, then re-attaches
/// the same page. A page that is still in a Window must keep its view model; it is released only once
/// it has been taken out of the tree.
/// </summary>
[TestFixture]
public class PageFactoryDetachTests
{
    private ServiceProvider _serviceProvider = null!;
    private PageFactory _sut = null!;
    private readonly List<Page> _unloadedEvents = [];

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        IPageRegistry registry = new PageRegistry(services);
        registry.MapPage<DetachTestPage, TrackablePageViewModel>();
        registry.MapPage<DetachTestPopup, TrackablePageViewModel>();
        services.AddSingleton(registry);

        // Invoke is an extension over Handle, so a substitute still runs the lifecycle method.
        services.AddSingleton(Substitute.For<ILifecycleExceptionHandler>());
        services.AddSingleton<ILogger<PageFactory>>(NullLogger<PageFactory>.Instance);
        services.AddSingleton<IPageFactory, PageFactory>();

        _serviceProvider = services.BuildServiceProvider();
        _sut = (PageFactory)_serviceProvider.GetRequiredService<IPageFactory>();
        _unloadedEvents.Clear();
        _sut.PageUnloaded += (_, page) => _unloadedEvents.Add(page);
    }

    [TearDown]
    public void TearDown() => _serviceProvider.Dispose();

    private (Page Page, TrackablePageViewModel ViewModel) Create<TPage>() where TPage : Page
    {
        var page = ((IPageFactory)_sut).CreatePage(new PageInfo(typeof(TPage)));
        return (page, (TrackablePageViewModel)page.BindingContext);
    }

    [Test]
    public void Unloaded_WhenPageIsStillInAWindow_KeepsViewModel()
    {
        // Given — the Activity was destroyed in the background; the page is still the Window's root
        var (page, viewModel) = Create<DetachTestPage>();
        _ = new Window(page);

        // When
        _sut.HandlePageUnloaded(page);

        // Then
        Assert.That(viewModel.DisposeCount, Is.EqualTo(0));
        Assert.That(viewModel.PageUnloadedCount, Is.EqualTo(0));
        Assert.That(_unloadedEvents, Is.Empty);
    }

    [Test]
    public void Unloaded_WhenPageIsStillInAWindow_LifecycleStillReachesViewModelAfterReattach()
    {
        // Given
        var (page, viewModel) = Create<DetachTestPage>();
        _ = new Window(page);
        _sut.HandlePageUnloaded(page);

        // When — the Activity is recreated
        _sut.HandlePageLoaded(page);
        _sut.HandlePageAppearing(page);

        // Then
        Assert.That(viewModel.AppearingCount, Is.EqualTo(1));
        Assert.That(viewModel.DisposeCount, Is.EqualTo(0));
    }

    [Test]
    public void Unloaded_ThenRootReplaced_ReleasesOnce()
    {
        // Given
        var (page, viewModel) = Create<DetachTestPage>();
        var window = new Window(page);
        _sut.HandlePageUnloaded(page);

        // When
        window.Page = new ContentPage();

        // Then
        Assert.That(page.Parent, Is.Null, "precondition: replacing the root clears its Parent");
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
        Assert.That(viewModel.PageUnloadedCount, Is.EqualTo(1));
        Assert.That(_unloadedEvents, Is.EqualTo([page]));
    }

    [Test]
    public void Unloaded_ThenAncestorRemoved_ReleasesChild()
    {
        // Given — a page whose container is later removed: the page's own Parent never changes
        // (same shape as a tab whose TabbedPage is popped; TabbedPage needs a dispatcher in tests)
        var (page, viewModel) = Create<DetachTestPage>();
        var container = new NavigationPage(page);
        var window = new Window(container);
        _sut.HandlePageUnloaded(page);
        Assert.That(viewModel.DisposeCount, Is.EqualTo(0), "precondition: kept while in the window");

        // When
        window.Page = new ContentPage();

        // Then
        Assert.That(page.Parent, Is.SameAs(container));
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void ParentClearedBeforeUnloaded_ReleasesOnUnloaded()
    {
        // Given — the usual pop order: removed from the tree, then the platform view detaches
        var (page, viewModel) = Create<DetachTestPage>();
        var window = new Window(page)
        {
            Page = new ContentPage()
        };

        // When
        _sut.HandlePageUnloaded(page);

        // Then
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Unloaded_Repeated_ThenRemoved_ReleasesOnce()
    {
        // Given — backgrounded twice
        var (page, viewModel) = Create<DetachTestPage>();
        var window = new Window(page);
        _sut.HandlePageUnloaded(page);
        _sut.HandlePageUnloaded(page);

        // When
        window.Page = new ContentPage();
        _sut.HandlePageUnloaded(page);

        // Then
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
        Assert.That(_unloadedEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public void Reloaded_ThenRemoved_ReleasedOnce()
    {
        // Given — detached, then re-attached
        var (page, viewModel) = Create<DetachTestPage>();
        var window = new Window(page);
        _sut.HandlePageUnloaded(page);
        _sut.HandlePageLoaded(page);

        // When — removed. Without a handler MAUI raises Unloaded itself when the page leaves the
        // Window; on a device the platform raises it. Either way it must release exactly once.
        window.Page = new ContentPage();
        _sut.HandlePageUnloaded(page);

        // Then
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
        Assert.That(_unloadedEvents, Has.Count.EqualTo(1));
    }

    [Test]
    public void ReleasePage_WhileDetached_StopsWatching()
    {
        // Given
        var (page, viewModel) = Create<DetachTestPage>();
        var window = new Window(page);
        _sut.HandlePageUnloaded(page);

        // When
        ((IPageFactory)_sut).ReleasePage(page);
        window.Page = new ContentPage();

        // Then — released by the lease only; no second dispose, no PageUnloaded
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
        Assert.That(_unloadedEvents, Is.Empty);
    }

    [Test]
    public void Popup_UnloadedWhileParented_KeptUntilParentCleared()
    {
        // Given — Mopups sets a popup's Parent to the window's root page and clears it on removal
        var (popup, viewModel) = Create<DetachTestPopup>();
        var root = new ContentPage();
        _ = new Window(root);
        popup.Parent = root;
        _sut.HandlePageUnloaded(popup);
        Assert.That(_unloadedEvents, Is.Empty, "a backgrounded popup must stay tracked");

        // When — dismissed (RemoveAsync)
        popup.Parent = null;

        // Then — PopupService relies on PageUnloaded to forget the popup
        Assert.That(_unloadedEvents, Is.EqualTo([popup]));
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
    }
}

internal class DetachTestPage : ContentPage { }

internal class DetachTestPopup : PopupPage { }
