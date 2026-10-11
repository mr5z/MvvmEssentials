using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;
using iOSPage = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page;
using iOSModalStyle = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class ModalServiceTests
{
    // -----------------------------------------------------------------------
    // WrapIfNeeded
    // -----------------------------------------------------------------------

    [Test]
    public void WrapIfNeeded_ByDefault_HostsPageInNavigationPage()
    {
        // Given
        var page = new ContentPage();

        // When
        var presented = ModalService.WrapIfNeeded(page);

        // Then
        Assert.That(presented, Is.InstanceOf<NavigationPage>());
        Assert.That(((NavigationPage)presented).RootPage, Is.SameAs(page));
    }

    [Test]
    public void WrapIfNeeded_WhenWithNavigationIsFalse_ReturnsPageAsIs()
    {
        // Given
        var page = new ContentPage();
        Modal.SetWithNavigation(page, false);

        // When
        var presented = ModalService.WrapIfNeeded(page);

        // Then
        Assert.That(presented, Is.SameAs(page));
    }

    [Test]
    public void WrapIfNeeded_WhenPageHasItsOwnNavigation_ReturnsPageAsIs()
    {
        var navigationPage = new NavigationPage(new ContentPage());
        var tabbedPage = new TabbedPage();
        var flyoutPage = new FlyoutPage { Flyout = new ContentPage { Title = "menu" }, Detail = new ContentPage() };

        Assert.Multiple(() =>
        {
            Assert.That(ModalService.WrapIfNeeded(navigationPage), Is.SameAs(navigationPage));
            Assert.That(ModalService.WrapIfNeeded(tabbedPage), Is.SameAs(tabbedPage));
            Assert.That(ModalService.WrapIfNeeded(flyoutPage), Is.SameAs(flyoutPage));
        });
    }

    [Test]
    public void WrapIfNeeded_WhenPageSetsIosPresentationStyle_CopiesItToTheWrapper()
    {
        // Given
        var page = new ContentPage();
        page.SetValue(iOSPage.ModalPresentationStyleProperty, iOSModalStyle.PageSheet);

        // When
        var presented = ModalService.WrapIfNeeded(page);

        // Then
        Assert.That(presented, Is.Not.SameAs(page));
        Assert.That(presented.GetValue(iOSPage.ModalPresentationStyleProperty), Is.EqualTo(iOSModalStyle.PageSheet));
    }

    [Test]
    public void WrapIfNeeded_WhenPageDoesNotSetIosPresentationStyle_LeavesWrapperDefault()
    {
        // When
        var presented = ModalService.WrapIfNeeded(new ContentPage());

        // Then
        Assert.That(presented.IsSet(iOSPage.ModalPresentationStyleProperty), Is.False);
    }

    // -----------------------------------------------------------------------
    // Scope lifetime: the wrapper must not cause the page's scope to be released
    // -----------------------------------------------------------------------

    [Test]
    public void Lease_WhenWrapperIsCommitted_KeepsTheWrappedPage()
    {
        // Given
        var pageFactory = Substitute.For<IPageFactory>();
        var page = new ContentPage();
        pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters?>()).Returns(page);
        var lease = new PageLease(pageFactory);

        // When
        var created = lease.Create(new PageInfo(typeof(FakePage)), null);
        lease.Commit(ModalService.WrapIfNeeded(created));
        ((IDisposable)lease).Dispose();

        // Then
        pageFactory.DidNotReceive().ReleasePage(page);
    }

    [Test]
    public void Lease_WhenPushFailsBeforeCommit_ReleasesTheWrappedPage()
    {
        // Given
        var pageFactory = Substitute.For<IPageFactory>();
        var page = new ContentPage();
        pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters?>()).Returns(page);
        var lease = new PageLease(pageFactory);

        // When: no Commit, as in ModalService when PushModalAsync throws
        var created = lease.Create(new PageInfo(typeof(FakePage)), null);
        _ = ModalService.WrapIfNeeded(created);
        ((IDisposable)lease).Dispose();

        // Then
        pageFactory.Received(1).ReleasePage(page);
    }

    // -----------------------------------------------------------------------
    // Android Back: pops ModalService didn't start go through the view model
    // -----------------------------------------------------------------------

    private sealed class BackTestContext
    {
        public required ModalService Sut { get; init; }
        public required Window Window { get; init; }
        public required List<Action> Dispatched { get; init; }

        public IModalService Service => Sut;

        // What Android Back does: MAUI pops the top modal itself.
        public Task PressBackAsync() => Window.Navigation.PopModalAsync();

        // Run what the service handed to the UI dispatcher, after the cancelled pop has returned.
        public void RunDispatched()
        {
            var actions = Dispatched.ToList();
            Dispatched.Clear();
            actions.ForEach(action => action());
        }
    }

    private static BackTestContext CreateBackTestContext(params Page[] modalPages)
        => CreateBackTestContext(interceptsBackNavigation: true, modalPages);

    // Each PresentAsync call creates the next page in modalPages.
    private static BackTestContext CreateBackTestContext(bool interceptsBackNavigation, params Page[] modalPages)
    {
        var pageFactory = Substitute.For<IPageFactory>();
        pageFactory.GetPageTypesFromPath(Arg.Any<string>()).Returns([new PageInfo(typeof(FakePage))]);
        pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters?>())
            .Returns(modalPages[0], modalPages[1..]);

        var window = new Window(new ContentPage());
        var context = Substitute.For<IApplicationContext>();
        context.Windows.Returns([window]);

        var dispatched = new List<Action>();
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.Dispatch(Arg.Do<Action>(dispatched.Add)).Returns(true);

        var sut = new ModalService(NullLogger<ModalService>.Instance, pageFactory, context, dispatcher)
        {
            InterceptsBackNavigation = interceptsBackNavigation
        };

        return new BackTestContext { Sut = sut, Window = window, Dispatched = dispatched };
    }

    [Test]
    public async Task Back_WhenViewModelDeclines_KeepsModalOpen()
    {
        // Given
        var page = new ContentPage();
        var test = CreateBackTestContext(page);
        var viewModel = new TestModalViewModel(test.Service) { AllowDismiss = false };
        page.BindingContext = viewModel;
        await test.Service.PresentAsync("FakePage");

        // When
        await test.PressBackAsync();
        test.RunDispatched();

        // Then
        Assert.That(test.Window.Navigation.ModalStack, Has.Count.EqualTo(1));
        Assert.That(viewModel.CanDismissCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Back_WhenViewModelAllows_ClosesModalThroughService()
    {
        // Given
        var page = new ContentPage();
        var test = CreateBackTestContext(page);
        var viewModel = new TestModalViewModel(test.Service);
        page.BindingContext = viewModel;
        await test.Service.PresentAsync("FakePage");

        // When
        await test.PressBackAsync();
        Assert.That(test.Window.Navigation.ModalStack, Has.Count.EqualTo(1), "Back alone must not close it");
        test.RunDispatched();

        // Then
        Assert.That(test.Window.Navigation.ModalStack, Is.Empty);
        Assert.That(viewModel.CanDismissCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Back_WhenPageIsNotHostedInNavigationPage_IsAlsoRouted()
    {
        // Given
        var page = new ContentPage();
        Modal.SetWithNavigation(page, false);
        var test = CreateBackTestContext(page);
        var viewModel = new TestModalViewModel(test.Service) { AllowDismiss = false };
        page.BindingContext = viewModel;
        await test.Service.PresentAsync("FakePage");

        // When
        await test.PressBackAsync();
        test.RunDispatched();

        // Then
        Assert.That(test.Window.Navigation.ModalStack, Has.Count.EqualTo(1));
        Assert.That(viewModel.CanDismissCallCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Back_WithoutModalViewModel_ClosesAsBefore()
    {
        // Given
        var page = new ContentPage { BindingContext = new object() };
        var test = CreateBackTestContext(page);
        await test.Service.PresentAsync("FakePage");

        // When
        await test.PressBackAsync();

        // Then
        Assert.That(test.Window.Navigation.ModalStack, Is.Empty);
        Assert.That(test.Dispatched, Is.Empty);
    }

    [Test]
    public async Task Back_WhenInterceptionIsOff_ClosesWithoutAskingViewModel()
    {
        // Given: iOS, where a pop may report a sheet the user already swiped away
        var page = new ContentPage();
        var test = CreateBackTestContext(interceptsBackNavigation: false, page);
        var viewModel = new TestModalViewModel(test.Service) { AllowDismiss = false };
        page.BindingContext = viewModel;
        await test.Service.PresentAsync("FakePage");

        // When
        await test.PressBackAsync();

        // Then
        Assert.That(test.Window.Navigation.ModalStack, Is.Empty);
        Assert.That(viewModel.CanDismissCallCount, Is.Zero);
    }

    [Test]
    public async Task DismissAsync_IsNotIntercepted()
    {
        // Given: the service's own pop must never be routed back to the view model
        var page = new ContentPage();
        var test = CreateBackTestContext(page);
        var viewModel = new TestModalViewModel(test.Service) { AllowDismiss = false };
        page.BindingContext = viewModel;
        await test.Service.PresentAsync("FakePage");

        // When
        var result = await test.Service.DismissAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(test.Window.Navigation.ModalStack, Is.Empty);
        Assert.That(viewModel.CanDismissCallCount, Is.Zero);
    }

    [Test]
    public async Task DismissAllAsync_IsNotIntercepted()
    {
        // Given: two modals backed by view models that would decline
        var first = new ContentPage();
        var second = new ContentPage();
        var test = CreateBackTestContext(first, second);
        first.BindingContext = new TestModalViewModel(test.Service) { AllowDismiss = false };
        second.BindingContext = new TestModalViewModel(test.Service) { AllowDismiss = false };
        await test.Service.PresentAsync("FakePage");
        await test.Service.PresentAsync("FakePage");

        // When
        var result = await test.Service.DismissAllAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(test.Window.Navigation.ModalStack, Is.Empty);
        Assert.That(test.Dispatched, Is.Empty);
    }

    [Test]
    public async Task PresentAsync_TwiceOnSameWindow_HandlesEachBackPressOnce()
    {
        // Given: two presentations on one window must not subscribe twice
        var first = new ContentPage();
        var second = new ContentPage();
        var test = CreateBackTestContext(first, second);
        first.BindingContext = new TestModalViewModel(test.Service);
        second.BindingContext = new TestModalViewModel(test.Service) { AllowDismiss = false };
        await test.Service.PresentAsync("FakePage");
        await test.Service.PresentAsync("FakePage");

        // When
        await test.PressBackAsync();

        // Then: counted before running, because Dismiss() would merge duplicate calls and hide the bug
        Assert.That(test.Dispatched, Has.Count.EqualTo(1));
    }
}