using Microsoft.Extensions.Logging.Abstractions;
using Mopups.Interfaces;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PopupPage = Mopups.Pages.PopupPage;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class PopupServiceTests
{
    private IPopupNavigation _popupNavigation = null!;
    private IPageFactory _pageFactory = null!;
    private IPageRegistry _pageRegistry = null!;
    private IPopupService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _popupNavigation = Substitute.For<IPopupNavigation>();
        _pageFactory = Substitute.For<IPageFactory>();
        _pageRegistry = Substitute.For<IPageRegistry>();

        // NullLogger avoids Castle DynamicProxy issues with ILogger<PopupService>
        // where PopupService is internal
        _sut = new PopupService(
            NullLogger<PopupService>.Instance,
            _popupNavigation,
            _pageFactory,
            _pageRegistry);
    }

    // -----------------------------------------------------------------------
    // PresentAsync — resolution failures
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WhenGetPageTypesThrows_ReturnsGeneralFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
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
        _pageRegistry.ResolvePageType("UnknownPopup").Returns((Type?)null);

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
        _pageRegistry.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        await _sut.PresentAsync("ConfirmPopup");

        _pageRegistry.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        await _sut.PresentAsync("ConfirmPopup");

        _pageRegistry.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

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
            .GetPageTypesFromPath<PopupPage>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(PopupPage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(popupPage);
        await _sut.PresentAsync("ConfirmPopup");
        _pageRegistry.ResolvePageType("ConfirmPopup").Returns(typeof(PopupPage));

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
}