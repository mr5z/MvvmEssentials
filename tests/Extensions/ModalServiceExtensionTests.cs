using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Presentations;
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

    // Completes the presentation the extension passes, the way the modal closing would
    private void PresentCompletesWith(Action<Presentation<TestModalResult>> complete)
    {
        _modalService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                if (callInfo.Arg<INavigationParameters>()
                    ?.TryGetValue<Presentation<TestModalResult>>(
                        NavigationHints.Presentation, out var presentation) == true)
                {
                    complete(presentation!);
                }
                return Result.Ok();
            });
    }

    private static void CloseWith(Presentation<TestModalResult> presentation, TestModalResult result)
    {
        presentation.SetPendingResult(result);
        presentation.OnClosed();
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
        PresentCompletesWith(p => CloseWith(p, new TestModalResult(true)));

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
        PresentCompletesWith(p => CloseWith(p, expected));

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
        PresentCompletesWith(p => CloseWith(p, new TestModalResult(true)));

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
        PresentCompletesWith(p => CloseWith(p, expected));

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
        PresentCompletesWith(p => p.OnClosed());

        // When
        var result = await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task PresentAsync_WithResult_WhenCancelled_DoesNotDismissAgain()
    {
        // Given — the modal is already gone; dismissing would pop whatever is on top now
        PresentCompletesWith(p => p.OnClosed());

        // When
        await _modalService.PresentAsync<TestModalViewModel, TestModalResult>();

        // Then
        await _modalService.DidNotReceive().DismissAsync(Arg.Any<bool>());
    }
}
