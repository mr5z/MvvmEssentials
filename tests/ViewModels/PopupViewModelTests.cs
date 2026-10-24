using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

/// <summary>
/// PopupViewModel against its Presentation; the platform work (IPresenter) is substituted.
/// </summary>
[TestFixture]
public class PopupViewModelTests
{
    private IPopupService _popupService = null!;
    private IPresenter _presenter = null!;
    private Presentation<TestPopupResult> _presentation = null!;
    private TestPopupViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _popupService = Substitute.For<IPopupService>();
        _presenter = Substitute.For<IPresenter>();
        _presentation = new Presentation<TestPopupResult>();
        var page = new TestPopupPage();
        _presentation.Open(_presenter, page, page);

        // What PopupService passes the popup's view model
        _sut = new TestPopupViewModel(_popupService);
        ((IParametersSet)_sut).OnParametersSet(new NavigationParameters { { NavigationHints.Presentation, _presentation } });
    }

    private void PresenterDismisses(IResult result) => _presenter.DismissAsync(_presentation).Returns(result);

    // -----------------------------------------------------------------------
    // PageName and defaults
    // -----------------------------------------------------------------------

    [Test]
    public void PageName_ReturnsPopupSuffix_NotPageSuffix()
    {
        // TestPopupViewModel → "TestPopupPopup"
        Assert.That(_sut.PageName, Is.EqualTo("TestPopupPopup"));
    }

    [Test]
    public void ShouldDismissOnBackButtonPressed_DefaultsToTrue()
    {
        Assert.That(_sut.PublicShouldDismissOnBackButtonPressed, Is.True);
    }

    [Test]
    public void ShouldDismissOnBackgroundTapped_DefaultsToTrue()
    {
        Assert.That(new TestPopupViewModel(_popupService).PublicShouldDismissOnBackgroundTapped, Is.True);
    }

    // -----------------------------------------------------------------------
    // Dismiss with result
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissWithResult_WhenPopupCloses_GivesTheCallerTheResult()
    {
        // Given
        PresenterDismisses(Result.Ok());
        var expected = new TestPopupResult(true);

        // When
        await _sut.PublicDismissWithResult(expected);

        // Then
        Assert.That(await _presentation.Result, Is.EqualTo(expected));
    }

    [Test]
    public async Task DismissWithResult_WhenThePopupCannotClose_LeavesTheCallerWaiting()
    {
        // Given
        PresenterDismisses(Result.Fail(ErrorCode.General, "error"));

        // When
        var result = await _sut.PublicDismissWithResult(new TestPopupResult(false));

        // Then: the popup is still on screen, so the caller keeps waiting (it used to receive the failure)
        Assert.That(result.IsFailure, Is.True);
        Assert.That(_presentation.Result.IsCompleted, Is.False);
    }

    [Test]
    public async Task DismissWithResult_DoesNotConsultCanDismiss()
    {
        // Given
        _sut.AllowDismiss = false;
        PresenterDismisses(Result.Ok());

        // When
        await _sut.PublicDismissWithResult(new TestPopupResult(true));

        // Then
        Assert.That(_sut.CanDismissCallCount, Is.Zero);
        Assert.That(_presentation.Result.IsCompletedSuccessfully, Is.True);
    }

    // -----------------------------------------------------------------------
    // DismissCommand
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissCommand_WhenPopupCloses_CancelsTheCaller()
    {
        // Given
        PresenterDismisses(Result.Ok());

        // When
        await _sut.DismissCommand.ExecuteAsync(null);

        // Then
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public async Task DismissCommand_WhenThePopupCannotClose_LeavesTheCallerWaiting()
    {
        // Given
        PresenterDismisses(Result.Fail(ErrorCode.General, "dismiss failed"));

        // When
        await _sut.DismissCommand.ExecuteAsync(null);

        // Then
        Assert.That(_presentation.Result.IsCompleted, Is.False);
    }

    [Test]
    public async Task Dismiss_WhenCanDismissDeclines_KeepsPopupOpen()
    {
        // Given
        _sut.AllowDismiss = false;

        // When
        var dismissed = await _sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.False);
        await _presenter.DidNotReceive().DismissAsync(Arg.Any<Presentation>());
        Assert.That(_presentation.Result.IsCompleted, Is.False);
    }

    // -----------------------------------------------------------------------
    // Platform dismissal: background tap and Back go through the view model
    // -----------------------------------------------------------------------

    [Test]
    public async Task BackgroundTap_DismissesThroughTheViewModel()
    {
        // Given
        var page = new TestPopupPage { BindingContext = _sut };
        PresenterDismisses(Result.Ok());

        // When
        var handled = page.TapBackground();
        await WaitForAsync(() => _presentation.Result.IsCompleted);

        // Then
        Assert.That(handled, Is.True);
        Assert.That(_sut.CanDismissCallCount, Is.EqualTo(1));
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public async Task BackgroundTap_WhenCanDismissDeclines_KeepsPopupOpen()
    {
        // Given
        var page = new TestPopupPage { BindingContext = _sut };
        _sut.AllowDismiss = false;

        // When
        page.TapBackground();
        await Task.Delay(20);

        // Then
        await _presenter.DidNotReceive().DismissAsync(Arg.Any<Presentation>());
        Assert.That(_presentation.Result.IsCompleted, Is.False);
    }

    [Test]
    public async Task BackgroundTap_WhenNotAllowedToDismiss_DoesNothing()
    {
        // Given
        var page = new TestPopupPage { BindingContext = _sut };
        _sut.DismissOnBackgroundTap = false;

        // When
        var handled = page.TapBackground();
        await Task.Delay(20);

        // Then
        Assert.That(handled, Is.True, "the tap is still swallowed");
        Assert.That(_sut.CanDismissCallCount, Is.Zero);
        await _presenter.DidNotReceive().DismissAsync(Arg.Any<Presentation>());
    }

    [Test]
    public async Task Back_DismissesThroughTheViewModel()
    {
        // Given
        var page = new TestPopupPage { BindingContext = _sut };
        PresenterDismisses(Result.Ok());

        // When
        page.PressBack();
        await WaitForAsync(() => _presentation.Result.IsCompleted);

        // Then
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public void PopupRemovedBySystem_CancelsTheCaller()
    {
        // When: PopupService reports the removal (PageUnloaded)
        _presentation.OnClosed();

        // Then
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    // -----------------------------------------------------------------------
    // Not presented with IPopupService
    // -----------------------------------------------------------------------

    [Test]
    public async Task NotPresented_DismissClosesTheTopPopup()
    {
        // Given
        var sut = new TestPopupViewModel(_popupService);
        ((IParametersSet)sut).OnParametersSet(new NavigationParameters());
        _popupService.DismissAsync().Returns(Result.Ok());

        // When
        var dismissed = await sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.True);
        await _popupService.Received(1).DismissAsync(Arg.Any<string?>(), Arg.Any<bool>());
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (var i = 0; i < 100 && condition() == false; i++)
        {
            await Task.Delay(10);
        }

        Assert.That(condition(), Is.True, "timed out");
    }
}
