using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.Mopups.Contracts;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PopupPage = Nkraft.Mopups.Pages.PopupPage;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class PopupServiceTests
{
    private IPopupNavigation _popupNavigation = null!;
    private IPageFactory _pageFactory = null!;
    private IPageCatalog _pageCatalog = null!;
    private IPopupService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _popupNavigation = Substitute.For<IPopupNavigation>();
        _pageFactory = Substitute.For<IPageFactory>();
        _pageCatalog = Substitute.For<IPageCatalog>();

        // NullLogger avoids Castle DynamicProxy issues with ILogger<PopupService>
        // where PopupService is internal
        _sut = new PopupService(
            NullLogger<PopupService>.Instance,
            _popupNavigation,
            _pageFactory,
            _pageCatalog);
    }

    // -----------------------------------------------------------------------
    // PresentAsync — resolution failures
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WhenGetPageTypesThrows_ReturnsGeneralFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Throws(new InvalidOperationException("boom"));

        // When
        var result = await _sut.PresentAsync("UnknownPopup");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }

    [Test]
    public async Task PresentAsync_WhenMultiplePagesResolved_ReturnsInvalidParameterFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage)), new PageInfo(typeof(PopupPage))]);

        // When
        var result = await _sut.PresentAsync("AmbiguousPopup");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidParameter));
    }

    // -----------------------------------------------------------------------
    // PresentAsync — happy path
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WithSinglePageResolved_ReturnsSuccess()
    {
        // Given
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);

        // When
        var result = await _sut.PresentAsync("ConfirmPopup");

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task PresentAsync_WithSinglePageResolved_PushesThatPageThroughPopupNavigation()
    {
        // Given
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);

        // When
        await _sut.PresentAsync("ConfirmPopup", animated: true);

        // Then
        await _popupNavigation.Received(1).PushAsync(popupPage, true);
    }

    [Test]
    public async Task PresentAsync_WhenPushAsyncThrows_ReturnsGeneralFailure()
    {
        // Given
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        _popupNavigation
            .PushAsync(Arg.Any<PopupPage>(), Arg.Any<bool>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        // When
        var result = await _sut.PresentAsync("ConfirmPopup");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }
    
    [Test]
    public async Task PresentAsync_WhenPushAsyncThrows_ReleasesCreatedPage()
    {
        // Given — the page never enters the tree, so Unloaded won't dispose its scope
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        _popupNavigation
            .PushAsync(Arg.Any<PopupPage>(), Arg.Any<bool>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        // When
        await _sut.PresentAsync("ConfirmPopup");

        // Then
        _pageFactory.Received(1).ReleasePage(popupPage);
    }

    [Test]
    public async Task PresentAsync_OnSuccess_DoesNotReleasePresentedPage()
    {
        // Given
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);

        // When
        await _sut.PresentAsync("ConfirmPopup");

        // Then
        _pageFactory.DidNotReceive().ReleasePage(popupPage);
    }

    // -----------------------------------------------------------------------
    // DismissAsync — no name specified (dismiss current)
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissAsync_WithNoPopupName_ReturnsSuccess()
    {
        // When
        var result = await _sut.DismissAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DismissAsync_WithNoPopupName_PopsCurrentPopup()
    {
        // When
        await _sut.DismissAsync(animated: false);

        // Then
        await _popupNavigation.Received(1).PopAsync(false);
    }

    [Test]
    public async Task DismissAsync_WithNoPopupName_WhenPopAsyncThrows_ReturnsGeneralFailure()
    {
        // Given
        _popupNavigation.PopAsync(Arg.Any<bool>()).ThrowsAsync(new InvalidOperationException("boom"));

        // When
        var result = await _sut.DismissAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }

    // -----------------------------------------------------------------------
    // DismissAsync — named popup
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissAsync_WhenPopupNameNotRegistered_ReturnsGeneralFailure()
    {
        // Given
        _pageCatalog.ResolvePageType("UnknownPopup").Returns((Type?)null);

        // When
        var result = await _sut.DismissAsync("UnknownPopup");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }

    [Test]
    public async Task DismissAsync_WhenPopupNotCurrentlyActive_ReturnsInvalidStateFailure()
    {
        // Given — registered type, but never presented, so it isn't tracked as active
        _pageCatalog.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

        // When
        var result = await _sut.DismissAsync("ConfirmPopup");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    [Test]
    public async Task DismissAsync_WhenPopupIsActive_ReturnsSuccess()
    {
        // Given — present a popup first so it's tracked as active
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        await _sut.PresentAsync("ConfirmPopup");

        _pageCatalog.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

        // When
        var result = await _sut.DismissAsync("ConfirmPopup");

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DismissAsync_WhenPopupIsActive_RemovesThatSpecificPageThroughPopupNavigation()
    {
        // Given — present a popup first so it's tracked as active
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        await _sut.PresentAsync("ConfirmPopup");

        _pageCatalog.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

        // When
        await _sut.DismissAsync("ConfirmPopup", animated: true);

        // Then
        await _popupNavigation.Received(1).RemovePageAsync(popupPage, true);
    }

    // -----------------------------------------------------------------------
    // DismissAllAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissAllAsync_Succeeds_ReturnsSuccess()
    {
        // When
        var result = await _sut.DismissAllAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DismissAllAsync_CallsPopAllAsyncOnPopupNavigation()
    {
        // When
        await _sut.DismissAllAsync(animated: false);

        // Then
        await _popupNavigation.Received(1).PopAllAsync(false);
    }

    [Test]
    public async Task DismissAllAsync_WhenThrows_ReturnsGeneralFailure()
    {
        // Given
        _popupNavigation.PopAllAsync(Arg.Any<bool>()).ThrowsAsync(new InvalidOperationException("boom"));

        // When
        var result = await _sut.DismissAllAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }

    // -----------------------------------------------------------------------
    // PageFactory_PageUnloaded — wired up in the constructor, fired by the
    // real IPageFactory when a native Page's Unloaded event fires
    // -----------------------------------------------------------------------

    private void RaisePageUnloaded(Page page) =>
        _pageFactory.PageUnloaded += Raise.Event<EventHandler<Page>>(_pageFactory, page);

    [Test]
    public async Task PageFactoryPageUnloaded_WhenAnActivePopupUnloads_RemovesItFromActivePopups()
    {
        // Given — present a popup so it's tracked as active
        var popupPage = new PopupPage();
        _pageFactory
            .GetPageTypesFromPath(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        await _sut.PresentAsync("ConfirmPopup");
        _pageCatalog.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

        // When — simulate MAUI unloading the popup natively
        RaisePageUnloaded(popupPage);

        // Then — no longer tracked as active, so a subsequent dismiss can't find it
        var result = await _sut.DismissAsync("ConfirmPopup");
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    [Test]
    public void PageFactoryPageUnloaded_WhenPopupWasNeverTracked_DoesNotThrow()
    {
        // Given — a PopupPage that was never presented through this service
        var untracked = new PopupPage();

        // Then
        Assert.DoesNotThrow(() => RaisePageUnloaded(untracked));
    }

    [Test]
    public void PageFactoryPageUnloaded_WhenUnloadedPageIsNotAPopupPage_DoesNotThrow()
    {
        // Given — a regular page unloading has nothing to do with popup tracking
        var regularPage = new Page();

        // Then
        Assert.DoesNotThrow(() => RaisePageUnloaded(regularPage));
    }

    // -----------------------------------------------------------------------
    // Presentations: the caller's result follows the popup page
    // -----------------------------------------------------------------------

    private async Task<(PopupPage Page, Presentation<TestPopupResult> Presentation)> PresentForResultAsync()
    {
        var popupPage = new PopupPage();
        _pageFactory.GetPageTypesFromPath(Arg.Any<string>()).Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>()).Returns(popupPage);

        var presentation = new Presentation<TestPopupResult>();
        var parameters = new NavigationParameters { { NavigationHints.Presentation, presentation } };
        var presented = await _sut.PresentAsync("ConfirmPopup", parameters);
        Assert.That(presented.IsSuccess, Is.True);
        return (popupPage, presentation);
    }

    [Test]
    public async Task PresentAsync_OpensTheCallersPresentationOnThePopup()
    {
        // When
        var (popupPage, presentation) = await PresentForResultAsync();

        // Then
        Assert.That(presentation.Host, Is.SameAs(popupPage));
        Assert.That(presentation.IsClosed, Is.False);
    }

    [Test]
    public async Task PresentAsync_WithoutACaller_StillGivesThePageAPresentation()
    {
        // Given: PresentAsync(name), nobody awaits a result
        INavigationParameters? received = null;
        _pageFactory.GetPageTypesFromPath(Arg.Any<string>()).Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Do<INavigationParameters?>(p => received = p)).Returns(new PopupPage());

        // When
        await _sut.PresentAsync("ConfirmPopup");

        // Then: so Dismiss() still works through it
        Assert.That(received!.TryGetValue<Presentation>(NavigationHints.Presentation, out _), Is.True);
    }

    [Test]
    public async Task PageUnloaded_CompletesTheCallerAsCancelled()
    {
        // Given
        var (popupPage, presentation) = await PresentForResultAsync();

        // When: Mopups removed the popup (background tap, PopAsync, PopAllAsync)
        RaisePageUnloaded(popupPage);

        // Then
        Assert.That(presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public async Task Complete_RemovesThatPopupAndGivesTheCallerTheResult()
    {
        // Given
        var (popupPage, presentation) = await PresentForResultAsync();

        // When: what Dismiss(result) on the view model does; the presentation asks this service to remove
        // the page, then closes itself (in the app, the page's Unloaded closes it too)
        var dismissed = await presentation.CompleteAsync(new TestPopupResult(true));

        // Then
        Assert.That(dismissed.IsSuccess, Is.True);
        await _popupNavigation.Received(1).RemovePageAsync(popupPage, Arg.Any<bool>());
        Assert.That(await presentation.Result, Is.EqualTo(new TestPopupResult(true)));
    }

    [Test]
    public async Task PresenterDismiss_RemovesThePageButLeavesClosingToThePresentation()
    {
        // Given
        var (popupPage, presentation) = await PresentForResultAsync();

        // When: the presenter alone
        var dismissed = await ((IPresenter)_sut).DismissAsync(presentation);

        // Then: the page is gone; the presentation closes from Presentation.DismissAsync or PageUnloaded, not here
        Assert.That(dismissed.IsSuccess, Is.True);
        await _popupNavigation.Received(1).RemovePageAsync(popupPage, Arg.Any<bool>());
        Assert.That(presentation.IsClosed, Is.False);
    }

    [Test]
    public async Task PresenterDismiss_WhenMopupsThrows_LeavesTheCallerWaiting()
    {
        // Given
        var (popupPage, presentation) = await PresentForResultAsync();
        _popupNavigation.RemovePageAsync(popupPage, Arg.Any<bool>()).Returns(Task.FromException(new InvalidOperationException("boom")));

        // When
        var dismissed = await ((IPresenter)_sut).DismissAsync(presentation);

        // Then
        Assert.That(dismissed.ErrorCode, Is.EqualTo(ErrorCode.General));
        Assert.That(presentation.Result.IsCompleted, Is.False);
    }

    [Test]
    public async Task DismissAllAsync_CancelsEveryWaitingCaller()
    {
        // Given
        var (_, presentation) = await PresentForResultAsync();

        // When
        await _sut.DismissAllAsync();

        // Then
        Assert.That(presentation.Result.IsCanceled, Is.True);
    }
}
