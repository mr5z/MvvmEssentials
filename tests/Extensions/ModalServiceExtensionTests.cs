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
    // PresentAsync<TViewModel, TResult>()
    // -----------------------------------------------------------------------

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