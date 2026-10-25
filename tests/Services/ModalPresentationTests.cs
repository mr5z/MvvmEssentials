using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using Nkraft.MvvmEssentials.ViewModels;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

/// <summary>
/// A modal with several pages, end to end: real ModalService and Window, substituted page factory.
/// </summary>
[TestFixture]
public class ModalPresentationTests
{
    private IPageFactory _pageFactory = null!;
    private IApplicationContext _applicationContext = null!;
    private Window _window = null!;
    private NavigationPage _mainPage = null!;
    private List<Action> _dispatched = null!;
    private ModalService _service = null!;

    private IModalService Modals => _service;

    [SetUp]
    public void SetUp()
    {
        _pageFactory = Substitute.For<IPageFactory>();
        _applicationContext = Substitute.For<IApplicationContext>();
        _mainPage = new NavigationPage(new ContentPage());
        _window = new Window(_mainPage);
        _applicationContext.Windows.Returns([_window]);
        _applicationContext.MainPage.Returns(_mainPage);

        _dispatched = [];
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.Dispatch(Arg.Do<Action>(_dispatched.Add)).Returns(true);
        _service = CreateService(interceptsBackNavigation: false, dispatcher);

        DispatcherProvider.SetCurrent(new TestDispatcherProvider());
    }

    private ModalService CreateService(bool interceptsBackNavigation, IDispatcher? dispatcher = null) => new(
        NullLogger<ModalService>.Instance, _pageFactory, _applicationContext, dispatcher ?? Substitute.For<IDispatcher>())
    {
        InterceptsBackNavigation = interceptsBackNavigation
    };

    // The next page the factory creates, bound to a view model built from the parameters it receives,
    // as PageFactory does
    private ContentPage NextPage<TViewModel>(Func<TViewModel> createViewModel, Action<TViewModel>? received = null)
        where TViewModel : class, IParametersSet
    {
        var page = new ContentPage();
        _pageFactory.GetPageTypesFromPath(Arg.Any<string>()).Returns([new PageInfo(typeof(FakePage))]);
        _pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters?>()).Returns(call =>
        {
            var viewModel = createViewModel();
            viewModel.OnParametersSet(call.Arg<INavigationParameters?>() ?? new NavigationParameters());
            page.BindingContext = viewModel;
            received?.Invoke(viewModel);
            return page;
        });
        return page;
    }

    // Presents the first page as PresentAsync<TestModalViewModel, TestModalResult>() does
    private async Task<(TestModalViewModel First, Task<Result<TestModalResult>> Caller)> PresentAsync(
        Action<ContentPage>? configure = null)
    {
        TestModalViewModel first = null!;
        var page = NextPage(() => new TestModalViewModel(Modals), vm => first = vm);
        configure?.Invoke(page);
        var caller = Modals.PresentAsync<TestModalViewModel, TestModalResult>(animated: false);

        // The caller awaits the modal's result, so wait for the modal to be on screen instead
        for (var i = 0; i < 100 && (first is null || _window.Navigation.ModalStack.Count == 0); i++)
        {
            await Task.Delay(10);
        }

        Assert.That(first, Is.Not.Null, "The modal wasn't presented");
        return (first, caller);
    }

    private NavigationPage Host => (NavigationPage)_window.Navigation.ModalStack[^1];

    private async Task<(TestModalSecondViewModel Second, ContentPage Page)> PushSecondAsync(TestModalViewModel from)
    {
        TestModalSecondViewModel second = null!;
        var page = NextPage(() => new TestModalSecondViewModel(Modals), vm => second = vm);
        var destination = new ModalDestination<TestModalResult>("TestModalSecondPage", new NavigationParameters { { "Id", 7 } });
        var pushed = await from.PublicPushAsync(destination);
        Assert.That(pushed.IsSuccess, Is.True, pushed.ErrorMessage);
        return (second, page);
    }

    // Replaces, then runs the UI loop turn the covered page's removal waits for
    private async Task<IResult> ReplaceAsync<TNext>(TestModalViewModel from)
        where TNext : IModalViewModel<TestModalResult>
    {
        var replacing = from.PublicReplaceAsync<TNext>();
        await RunDispatchedWhenQueuedAsync();
        return await replacing;
    }

    private async Task RunDispatchedWhenQueuedAsync()
    {
        for (var i = 0; i < 100 && _dispatched.Count == 0; i++)
        {
            await Task.Delay(10);
        }

        Assert.That(_dispatched, Is.Not.Empty, "Nothing was dispatched");
        RunDispatched();
    }

    // -----------------------------------------------------------------------
    // Pages inside the modal
    // -----------------------------------------------------------------------

    [Test]
    public async Task PushAsync_PushesInsideTheModalNotBehindIt()
    {
        // Given
        var (first, _) = await PresentAsync();

        // When
        var (_, secondPage) = await PushSecondAsync(first);

        // Then
        Assert.That(Host.CurrentPage, Is.SameAs(secondPage));
        Assert.That(_window.Navigation.ModalStack, Has.Count.EqualTo(1));
        Assert.That(_mainPage.Navigation.NavigationStack, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PushAsync_KeepsThePushedPageScope()
    {
        // Given
        var (first, _) = await PresentAsync();

        // When
        var (_, secondPage) = await PushSecondAsync(first);

        // Then
        _pageFactory.DidNotReceive().ReleasePage(secondPage);
    }

    [Test]
    public async Task PushedPage_DismissWithResult_GivesTheCallerTheResult()
    {
        // Given
        var (first, caller) = await PresentAsync();
        var (second, _) = await PushSecondAsync(first);

        // When
        var dismissed = await second.PublicDismissWithResult(new TestModalResult(true));

        // Then
        Assert.That(dismissed.IsSuccess, Is.True);
        Assert.That(_window.Navigation.ModalStack, Is.Empty);
        Assert.That((await caller).TryGetValue(out var value) && value == new TestModalResult(true), Is.True);
    }

    [Test]
    public async Task PushedPage_Dismiss_ClosesTheWholeModalAndCancelsTheCaller()
    {
        // Given
        var (first, caller) = await PresentAsync();
        var (second, _) = await PushSecondAsync(first);

        // When
        await second.Dismiss();

        // Then
        Assert.That(_window.Navigation.ModalStack, Is.Empty);
        Assert.That((await caller).ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task PopAsync_GoesBackAndLeavesTheCallerWaiting()
    {
        // Given
        var (first, caller) = await PresentAsync();
        var (second, _) = await PushSecondAsync(first);
        var firstPage = Host.RootPage;

        // When
        var popped = await second.PublicPopAsync();

        // Then
        Assert.That(popped.IsSuccess, Is.True);
        Assert.That(Host.CurrentPage, Is.SameAs(firstPage));
        Assert.That(caller.IsCompleted, Is.False);
    }

    [Test]
    public async Task PopAsync_OnTheFirstPage_ReturnsNotHandled()
    {
        // Given
        var (first, _) = await PresentAsync();

        // When
        var popped = await first.PublicPopAsync();

        // Then
        Assert.That(popped.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
        Assert.That(_window.Navigation.ModalStack, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task PushAsync_WhenTheModalHasNoNavigationPage_FailsWithNotSupported()
    {
        // Given: Modal.WithNavigation="False", as overlays use
        var (first, _) = await PresentAsync(page => Modal.SetWithNavigation(page, false));
        NextPage(() => new TestModalSecondViewModel(Modals));

        // When
        var pushed = await first.PublicPushAsync<TestModalSecondViewModel>();

        // Then
        Assert.That(pushed.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    [Test]
    public async Task ReplaceAsync_OnTheFirstPage_LeavesOnlyTheNewPage()
    {
        // Given
        var (first, caller) = await PresentAsync();
        var nextPage = NextPage(() => new TestModalSecondViewModel(Modals));

        // When
        var replaced = await ReplaceAsync<TestModalSecondViewModel>(first);

        // Then
        Assert.That(replaced.IsSuccess, Is.True, replaced.ErrorMessage);
        Assert.That(Host.Navigation.NavigationStack, Is.EqualTo(new[] { nextPage }));
        Assert.That(caller.IsCompleted, Is.False);
    }

    [Test]
    public async Task ReplaceAsync_OnAPushedPage_RemovesOnlyThatPage()
    {
        // Given
        var (first, _) = await PresentAsync();
        var firstPage = Host.RootPage;
        var (second, _) = await PushSecondAsync(first);
        var thirdPage = NextPage(() => new TestModalSecondViewModel(Modals));

        // When
        await ReplaceAsync<TestModalSecondViewModel>(second);

        // Then
        Assert.That(Host.Navigation.NavigationStack, Is.EqualTo(new Page[] { firstPage, thirdPage }));
    }

    [Test]
    public async Task ReplaceAsync_ThenPopAsync_ReturnsNotHandled()
    {
        // Given: the replaced first page can't be gone back to
        var (first, _) = await PresentAsync();
        TestModalSecondViewModel next = null!;
        NextPage(() => new TestModalSecondViewModel(Modals), vm => next = vm);
        await ReplaceAsync<TestModalSecondViewModel>(first);

        // When
        var popped = await next.PublicPopAsync();

        // Then
        Assert.That(popped.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
    }

    [Test]
    public async Task ReplacingPage_DismissWithResult_GivesTheCallerTheResult()
    {
        // Given
        var (first, caller) = await PresentAsync();
        TestModalSecondViewModel next = null!;
        NextPage(() => new TestModalSecondViewModel(Modals), vm => next = vm);
        await ReplaceAsync<TestModalSecondViewModel>(first);

        // When
        await next.PublicDismissWithResult(new TestModalResult(true));

        // Then
        Assert.That(_window.Navigation.ModalStack, Is.Empty);
        Assert.That((await caller).TryGetValue(out var value) && value == new TestModalResult(true), Is.True);
    }

    [Test]
    public async Task ReplaceAsync_ShowsTheNewPageBeforeTheCoveredPageGoes()
    {
        // Given
        var (first, _) = await PresentAsync();
        var firstPage = Host.RootPage;
        var nextPage = NextPage(() => new TestModalSecondViewModel(Modals));

        // When: pushed, but the UI loop turn the removal waits for hasn't run
        var replacing = first.PublicReplaceAsync<TestModalSecondViewModel>();
        for (var i = 0; i < 100 && _dispatched.Count == 0; i++)
        {
            await Task.Delay(10);
        }

        // Then
        Assert.That(Host.CurrentPage, Is.SameAs(nextPage));
        Assert.That(Host.Navigation.NavigationStack, Is.EqualTo(new Page[] { firstPage, nextPage }));
        Assert.That(replacing.IsCompleted, Is.False);

        RunDispatched();
        await replacing;
        Assert.That(Host.Navigation.NavigationStack, Is.EqualTo(new[] { nextPage }));
    }

    [Test]
    public async Task ReplaceAsync_WhenTheModalClosesBeforeThatTurn_LeavesItAlone()
    {
        // Given
        var (first, caller) = await PresentAsync();
        NextPage(() => new TestModalSecondViewModel(Modals));
        var replacing = first.PublicReplaceAsync<TestModalSecondViewModel>();
        for (var i = 0; i < 100 && _dispatched.Count == 0; i++)
        {
            await Task.Delay(10);
        }

        // When: the modal goes (iOS swipe-down, a direct pop) before the removal runs
        await _window.Navigation.PopModalAsync(animated: false);
        RunDispatched();

        // Then
        var replaced = await replacing;
        Assert.That(replaced.IsSuccess, Is.True, replaced.ErrorMessage);
        Assert.That(_window.Navigation.ModalStack, Is.Empty);
        Assert.That((await caller).ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task ReplaceAsync_WhenTheModalHasNoNavigationPage_FailsWithNotSupported()
    {
        // Given
        var (first, _) = await PresentAsync(page => Modal.SetWithNavigation(page, false));
        NextPage(() => new TestModalSecondViewModel(Modals));

        // When
        var replaced = await first.PublicReplaceAsync<TestModalSecondViewModel>();

        // Then
        Assert.That(replaced.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    // -----------------------------------------------------------------------
    // The modal going away by other routes
    // -----------------------------------------------------------------------

    [Test]
    public async Task SystemPop_CancelsTheCaller()
    {
        // Given
        var (_, caller) = await PresentAsync();

        // When: iOS swipe-down or a direct PopModalAsync; MAUI raises ModalPopped
        await _window.Navigation.PopModalAsync(animated: false);

        // Then
        Assert.That((await caller).ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task ReplacingTheWindowsPage_CancelsTheCaller()
    {
        // Given
        var (_, caller) = await PresentAsync();

        // When: absolute navigation; MAUI clears the modal stack without ModalPopped
        _window.Page = new ContentPage();

        // Then
        Assert.That((await caller).ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task Dismiss_WhenAnotherModalIsOnTop_FailsAndLeavesTheCallerWaiting()
    {
        // Given
        var (first, caller) = await PresentAsync();
        await _window.Navigation.PushModalAsync(new ContentPage(), animated: false);

        // When
        var dismissed = await first.PublicDismissWithResult(new TestModalResult(true));

        // Then
        Assert.That(dismissed.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
        Assert.That(caller.IsCompleted, Is.False);
    }

    [Test]
    public async Task AndroidBack_OnAPushedPage_GoesBackInsideTheModal()
    {
        // Given
        _service = CreateService(interceptsBackNavigation: true, DispatcherCapturing());
        var (first, caller) = await PresentAsync();
        await PushSecondAsync(first);
        var firstPage = Host.RootPage;

        // When: MAUI pops the modal itself on Back
        await _window.Navigation.PopModalAsync();
        RunDispatched();

        // Then
        Assert.That(_window.Navigation.ModalStack, Has.Count.EqualTo(1));
        Assert.That(Host.CurrentPage, Is.SameAs(firstPage));
        Assert.That(caller.IsCompleted, Is.False);
    }

    [Test]
    public async Task AndroidBack_OnTheFirstPage_AsksTheFirstPagesViewModel()
    {
        // Given
        _service = CreateService(interceptsBackNavigation: true, DispatcherCapturing());
        var (first, caller) = await PresentAsync();
        first.AllowDismiss = false;

        // When
        await _window.Navigation.PopModalAsync();
        RunDispatched();

        // Then
        Assert.That(first.CanDismissCallCount, Is.EqualTo(1));
        Assert.That(_window.Navigation.ModalStack, Has.Count.EqualTo(1));
        Assert.That(caller.IsCompleted, Is.False);
    }

    [Test]
    public async Task AndroidBack_AfterTheFirstPageWasReplaced_AsksTheNewFirstPagesViewModel()
    {
        // Given
        _service = CreateService(interceptsBackNavigation: true, DispatcherCapturing());
        var (first, caller) = await PresentAsync();
        TestModalSecondViewModel next = null!;
        NextPage(() => new TestModalSecondViewModel(Modals), vm => next = vm);
        await ReplaceAsync<TestModalSecondViewModel>(first);
        next.AllowDismiss = false;

        // When
        await _window.Navigation.PopModalAsync();
        RunDispatched();

        // Then
        Assert.That(first.CanDismissCallCount, Is.Zero);
        Assert.That(next.CanDismissCallCount, Is.EqualTo(1));
        Assert.That(_window.Navigation.ModalStack, Has.Count.EqualTo(1));
        Assert.That(caller.IsCompleted, Is.False);
    }

    // -----------------------------------------------------------------------
    // INavigationService while a modal is open
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigationService_WhileAModalIsOpen_RefusesToNavigateThePagesBehindIt()
    {
        // Given
        await PresentAsync();
        INavigationService navigation = new NavigationService(
            NullLogger<NavigationService>.Instance, _pageFactory, _applicationContext);

        // When
        var pushed = await navigation.NavigateAsync("SomePage");
        var back = await navigation.NavigateBackAsync();
        var root = await navigation.NavigateToRootAsync();

        // Then
        Assert.Multiple(() =>
        {
            Assert.That(pushed.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
            Assert.That(back.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
            Assert.That(root.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
            Assert.That(_mainPage.Navigation.NavigationStack, Has.Count.EqualTo(1));
        });
    }

    private IDispatcher DispatcherCapturing()
    {
        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.Dispatch(Arg.Do<Action>(_dispatched.Add)).Returns(true);
        return dispatcher;
    }

    private void RunDispatched()
    {
        var actions = _dispatched.ToList();
        _dispatched.Clear();
        actions.ForEach(action => action());
    }
}
