using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class ModalViewModelTests
{
    private IModalService _modalService = null!;
    private TestModalViewModel _sut = null!;
    private TaskCompletionSource<TestModalResult> _tcs = null!;

    [SetUp]
    public void SetUp()
    {
        _modalService = Substitute.For<IModalService>();
        _sut = new TestModalViewModel(_modalService);
        _tcs = new TaskCompletionSource<TestModalResult>();

        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.PopupCompletionParam, _tcs);
        ((IParametersSet)_sut).OnParametersSet(parameters);
    }

    private void SimulateUnload() => ((IDisposable)_sut).Dispose();

    // -----------------------------------------------------------------------
    // PageName
    // -----------------------------------------------------------------------

    [Test]
    public void PageName_UsesPageSuffix()
    {
        Assert.That(_sut.PageName, Is.EqualTo("TestModalPage"));
    }

    // -----------------------------------------------------------------------
    // Dismiss with result
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissWithResult_WhenServiceSucceeds_CompletesTask()
    {
        // Given
        _modalService.DismissAsync().Returns(Result.Ok());
        var expected = new TestModalResult(true);

        // When
        await _sut.PublicDismissWithResult(expected);

        // Then
        Assert.That(await _tcs.Task, Is.EqualTo(expected));
    }

    [Test]
    public async Task DismissWithResult_WhenUnloadFiresDuringDismissal_StillCompletesWithResult()
    {
        // Given — MAUI may unload the page before PopModalAsync returns
        _modalService.DismissAsync().Returns(_ =>
        {
            SimulateUnload();
            return Result.Ok();
        });
        var expected = new TestModalResult(true);

        // When
        await _sut.PublicDismissWithResult(expected);

        // Then
        Assert.That(_tcs.Task.IsCompletedSuccessfully, Is.True);
        Assert.That(await _tcs.Task, Is.EqualTo(expected));
    }

    [Test]
    public async Task DismissWithResult_WhenServiceFails_SetsExceptionOnCompletion()
    {
        // Given
        _modalService.DismissAsync().Returns(Result.Fail(ErrorCode.General, "boom"));

        // When
        await _sut.PublicDismissWithResult(new TestModalResult(true));

        // Then
        Assert.That(_tcs.Task.IsFaulted, Is.True);
    }

    // -----------------------------------------------------------------------
    // Dismiss without result
    // -----------------------------------------------------------------------

    [Test]
    public async Task Dismiss_WhenServiceSucceeds_CancelsTask()
    {
        // Given
        _modalService.DismissAsync().Returns(Result.Ok());

        // When
        var dismissed = await _sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.True);
        Assert.That(_tcs.Task.IsCanceled, Is.True);
    }

    [Test]
    public async Task Dismiss_WhenServiceFails_LeavesTaskPending()
    {
        // Given
        _modalService.DismissAsync().Returns(Result.Fail(ErrorCode.NotHandled, "none"));

        // When
        var dismissed = await _sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.False);
        Assert.That(_tcs.Task.IsCompleted, Is.False);
    }

    // -----------------------------------------------------------------------
    // System dismissal (Android back, iOS swipe-down)
    // -----------------------------------------------------------------------

    [Test]
    public void Dispose_WithoutDismiss_CancelsTask()
    {
        // When
        SimulateUnload();

        // Then
        Assert.That(_tcs.Task.IsCanceled, Is.True);
    }

    [Test]
    public void Dispose_WhenSubclassSkipsBaseCall_StillCancelsAndCallsSubclassHook()
    {
        // When
        SimulateUnload();

        // Then
        Assert.That(_tcs.Task.IsCanceled, Is.True);
        Assert.That(_sut.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Dispose_AfterResultWasSet_DoesNotOverrideResult()
    {
        // Given
        _modalService.DismissAsync().Returns(Result.Ok());
        var expected = new TestModalResult(false);
        await _sut.PublicDismissWithResult(expected);

        // When — unload arrives after dismissal finished
        SimulateUnload();

        // Then
        Assert.That(await _tcs.Task, Is.EqualTo(expected));
    }
}