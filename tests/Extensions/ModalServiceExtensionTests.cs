using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Extensions;

[TestFixture]
public class ModalServiceExtensionTests
{
    private IModalService _modalService = null!;

    [SetUp]
    public void SetUp()
    {
        _modalService = Substitute.For<IModalService>();
    }

    private void PresentCompletesWith(Action<TaskCompletionSource<TestModalResult>> complete)
    {
        _modalService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                if (callInfo.Arg<INavigationParameters>()
                    ?.TryGetValue<TaskCompletionSource<TestModalResult>>(
                        NavigationHints.PopupCompletionParam, out var tcs) == true)
                {
                    complete(tcs!);
                }
                return Result.Ok();
            });
    }

    // -----------------------------------------------------------------------
    // PresentAsync<TResult>(ModalDestination<TResult>)
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WithDestination_ForwardsNameParametersAndAnimated()
    {
        // Given — completion must resolve, otherwise the extension awaits forever
        var parameters = new NavigationParameters();
        var destination = new ModalDestination<TestModalResult>("EditPage", parameters);
        PresentCompletesWith(tcs => tcs.SetResult(new TestModalResult(true)));

        // When
        await _modalService.PresentAsync(destination, animated: false);

        // Then
        await _modalService.Received(1).PresentAsync("EditPage", parameters, false);
    }

    [Test]
    public async Task PresentAsync_WithDestination_WhenCompletionResolves_ReturnsSuccessWithValue()
    {
        // Given
        var destination = new ModalDestination<TestModalResult>("EditPage", new NavigationParameters());
        var expected = new TestModalResult(true);
        PresentCompletesWith(tcs => tcs.SetResult(expected));

        // When
        var result = await _modalService.PresentAsync(destination);

        // Then
        Assert.That(result.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public async Task PresentAsync_WithDestination_WhenNavigationFails_ReturnsFailure()
    {
        // Given
        var destination = new ModalDestination<TestModalResult>("EditPage", new NavigationParameters());
        _modalService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(Result.Fail(ErrorCode.General, "boom"));

        // When
        var result = await _modalService.PresentAsync(destination);

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // PresentAsync<TViewModel, TResult>()
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WithResult_UsesPageNamingConvention()
    {
        // Given
        PresentCompletesWith(tcs => tcs.SetResult(new TestModalResult(true)));

        // When
        await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        await _modalService.Received(1).PresentAsync("TestModalPage", Arg.Any<INavigationParameters>(), Arg.Any<bool>());
    }

    [Test]
    public async Task PresentAsync_WithResult_WhenNavigationFails_ReturnsFailure()
    {
        // Given
        _modalService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(Result.Fail(ErrorCode.General, "boom"));

        // When
        var result = await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task PresentAsync_WithResult_WhenCompletionResolves_ReturnsSuccessWithValue()
    {
        // Given
        var expected = new TestModalResult(true);
        PresentCompletesWith(tcs => tcs.SetResult(expected));

        // When
        var result = await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        Assert.That(result.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public async Task PresentAsync_WithResult_WhenCancelled_ReturnsCancelledFailure()
    {
        // Given
        PresentCompletesWith(tcs => tcs.SetCanceled());

        // When
        var result = await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task PresentAsync_WithResult_WhenCancelled_DoesNotDismissAgain()
    {
        // Given — the modal is already gone; dismissing would pop whatever is on top now
        PresentCompletesWith(tcs => tcs.SetCanceled());

        // When
        await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        await _modalService.DidNotReceive().DismissAsync(Arg.Any<bool>());
    }

    [Test]
    public async Task PresentAsync_WithResult_WhenCompletionFaults_ReturnsUnknownFailure()
    {
        // Given
        PresentCompletesWith(tcs => tcs.SetException(new InvalidOperationException("boom")));

        // When
        var result = await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.Unknown));
    }
}