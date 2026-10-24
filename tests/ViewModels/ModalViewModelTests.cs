using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

/// <summary>
/// ModalViewModel against its Presentation; the platform work (IStackPresenter) is substituted.
/// </summary>
[TestFixture]
public class ModalViewModelTests
{
    private IModalService _modalService = null!;
    private IStackPresenter _presenter = null!;
    private Presentation<TestModalResult> _presentation = null!;
    private TestModalViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _modalService = Substitute.For<IModalService>();
        _presenter = Substitute.For<IStackPresenter>();
        _presentation = new Presentation<TestModalResult>();
        _presentation.Open(_presenter, new ContentPage(), new ContentPage());
        _sut = CreatePage<TestModalViewModel>();
    }

    // What ModalService passes every page it creates for the modal
    private T CreatePage<T>() where T : TestModalViewModel
    {
        var page = (T)Activator.CreateInstance(typeof(T), _modalService)!;
        ((IParametersSet)page).OnParametersSet(new NavigationParameters { { NavigationHints.Presentation, _presentation } });
        return page;
    }

    private void PresenterDismisses(IResult result)
        => _presenter.DismissAsync(_presentation).Returns(result);

    private void SimulateUnload() => ((IDisposable)_sut).Dispose();

    [Test]
    public void PageName_UsesPageSuffix()
    {
        Assert.That(_sut.PageName, Is.EqualTo("TestModalPage"));
    }

    // -----------------------------------------------------------------------
    // Dismiss with result
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissWithResult_WhenModalCloses_GivesTheCallerTheResult()
    {
        // Given
        PresenterDismisses(Result.Ok());
        var expected = new TestModalResult(true);

        // When
        await _sut.PublicDismissWithResult(expected);

        // Then
        Assert.That(await _presentation.Result, Is.EqualTo(expected));
    }

    [Test]
    public async Task DismissWithResult_WhenMauiClosesTheModalDuringThePop_StillGivesTheResult()
    {
        // Given: ModalService closes the presentation from ModalPopped, before DismissAsync returns
        _presenter.DismissAsync(_presentation).Returns(_ =>
        {
            _presentation.OnClosed();
            return Result.Ok();
        });
        var expected = new TestModalResult(true);

        // When
        await _sut.PublicDismissWithResult(expected);

        // Then
        Assert.That(await _presentation.Result, Is.EqualTo(expected));
    }

    [Test]
    public async Task DismissWithResult_WhenTheModalCannotClose_LeavesTheCallerWaiting()
    {
        // Given
        PresenterDismisses(Result.Fail(ErrorCode.InvalidState, "another modal is on top"));

        // When
        var result = await _sut.PublicDismissWithResult(new TestModalResult(true));

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(_presentation.Result.IsCompleted, Is.False);
    }

    [Test]
    public async Task DismissWithResult_AfterAFailedAttempt_CancelDoesNotReturnTheOldResult()
    {
        // Given: the first close failed, so the modal is still open; the user then cancels
        PresenterDismisses(Result.Fail(ErrorCode.InvalidState, "another modal is on top"));
        await _sut.PublicDismissWithResult(new TestModalResult(true));
        PresenterDismisses(Result.Ok());

        // When
        await _sut.Dismiss();

        // Then
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public async Task DismissWithResult_DoesNotConsultCanDismiss()
    {
        // Given: saving must never be blocked by the discard guard
        _sut.AllowDismiss = false;
        PresenterDismisses(Result.Ok());

        // When
        await _sut.PublicDismissWithResult(new TestModalResult(true));

        // Then
        Assert.That(_sut.CanDismissCallCount, Is.Zero);
        Assert.That(_presentation.Result.IsCompletedSuccessfully, Is.True);
    }

    // -----------------------------------------------------------------------
    // Dismiss without result
    // -----------------------------------------------------------------------

    [Test]
    public async Task Dismiss_WhenModalCloses_CancelsTheCaller()
    {
        // Given
        PresenterDismisses(Result.Ok());

        // When
        var dismissed = await _sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.True);
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public async Task Dismiss_WhenTheModalCannotClose_LeavesTheCallerWaiting()
    {
        // Given
        PresenterDismisses(Result.Fail(ErrorCode.NotHandled, "none"));

        // When
        var dismissed = await _sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.False);
        Assert.That(_presentation.Result.IsCompleted, Is.False);
    }

    [Test]
    public async Task Dismiss_WhenCanDismissDeclines_KeepsModalOpen()
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

    [Test]
    public async Task Dismiss_WhileDismissalIsInProgress_SharesItsOutcome()
    {
        // Given: Back pressed twice while a "Discard changes?" prompt is open
        var decision = new TaskCompletionSource<bool>();
        _sut.PendingDecision = decision;
        PresenterDismisses(Result.Ok());

        // When
        var first = _sut.Dismiss();
        var second = _sut.Dismiss();
        decision.SetResult(true);

        // Then
        Assert.That(second, Is.SameAs(first));
        Assert.That(await first, Is.True);
        Assert.That(_sut.CanDismissCallCount, Is.EqualTo(1));
        await _presenter.Received(1).DismissAsync(Arg.Any<Presentation>());
    }

    [Test]
    public async Task Dismiss_AfterPreviousDismissalFinished_AsksAgain()
    {
        // Given
        _sut.AllowDismiss = false;
        await _sut.Dismiss();
        _sut.AllowDismiss = true;
        PresenterDismisses(Result.Ok());

        // When
        var dismissed = await _sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.True);
        Assert.That(_sut.CanDismissCallCount, Is.EqualTo(2));
    }

    // -----------------------------------------------------------------------
    // Closing is the modal's, not the view model's
    // -----------------------------------------------------------------------

    [Test]
    public void ModalClosedBySystem_CancelsTheCaller()
    {
        // When: Android Back, iOS swipe-down or a direct pop; ModalService reports MAUI's ModalPopped
        _presentation.OnClosed();

        // Then
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public void Dispose_DoesNotCompleteTheCaller()
    {
        // Given: a page popped inside the modal is disposed while the modal stays open

        // When
        SimulateUnload();

        // Then
        Assert.That(_presentation.Result.IsCompleted, Is.False);
        Assert.That(_sut.DisposeCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // Several pages in one modal
    // -----------------------------------------------------------------------

    [Test]
    public async Task PushAsync_WithADestination_PushesItInsideThisModal()
    {
        // Given
        var parameters = new NavigationParameters { { "Id", 7 } };
        var destination = new ModalDestination<TestModalResult>("TestModalSecondPage", parameters);
        _presenter.PushAsync(_presentation, "TestModalSecondPage", parameters, false).Returns(Result.Ok());

        // When
        var result = await _sut.PublicPushAsync(destination);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        await _presenter.Received(1).PushAsync(_presentation, "TestModalSecondPage", parameters, false);
    }

    [Test]
    public async Task PushAsync_ByViewModelType_PushesItsPage()
    {
        // Given
        _presenter.PushAsync(_presentation, Arg.Any<string>(), Arg.Any<INavigationParameters?>(), Arg.Any<bool>())
            .Returns(Result.Ok());

        // When
        await _sut.PublicPushAsync<TestModalSecondViewModel>();

        // Then
        await _presenter.Received(1).PushAsync(_presentation, "TestModalSecondPage", null, false);
    }

    [Test]
    public async Task PopAsync_GoesBackInsideThisModal()
    {
        // Given
        _presenter.PopAsync(_presentation, false).Returns(Result.Ok());

        // When
        var result = await _sut.PublicPopAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task DismissWithResult_FromAPushedPage_GivesTheCallerTheResult()
    {
        // Given: pages pushed inside the modal get the same presentation
        var page2 = CreatePage<TestModalSecondViewModel>();
        PresenterDismisses(Result.Ok());
        var expected = new TestModalResult(true);

        // When
        await page2.PublicDismissWithResult(expected);

        // Then
        Assert.That(await _presentation.Result, Is.EqualTo(expected));
    }

    // -----------------------------------------------------------------------
    // Not presented with IModalService
    // -----------------------------------------------------------------------

    [Test]
    public async Task NotPresented_PushAndPopFailWithInvalidState()
    {
        // Given
        var sut = new TestModalViewModel(_modalService);
        ((IParametersSet)sut).OnParametersSet(new NavigationParameters());

        // When
        var pushed = await sut.PublicPushAsync<TestModalSecondViewModel>();
        var popped = await sut.PublicPopAsync();

        // Then
        Assert.That(pushed.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
        Assert.That(popped.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    [Test]
    public async Task NotPresented_DismissClosesTheTopModal()
    {
        // Given: shown with a direct PushModalAsync
        var sut = new TestModalViewModel(_modalService);
        ((IParametersSet)sut).OnParametersSet(new NavigationParameters());
        _modalService.DismissAsync().Returns(Result.Ok());

        // When
        var dismissed = await sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.True);
        await _modalService.Received(1).DismissAsync(Arg.Any<bool>());
    }
}
