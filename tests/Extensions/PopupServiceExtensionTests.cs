using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Extensions;

[TestFixture]
public class PopupServiceExtensionTests
{
    private IPopupService _popupService = null!;

    [SetUp]
    public void SetUp()
    {
        _popupService = Substitute.For<IPopupService>();
    }

    private static void CompleteWith(INavigationParameters? parameters, TestPopupResult value)
    {
        if (parameters?.TryGetValue<TaskCompletionSource<TestPopupResult>>(
                NavigationHints.PopupCompletionParam, out var tcs) == true)
        {
            tcs.SetResult(value);
        }
    }

    // -----------------------------------------------------------------------
    // PresentAsync<TResult>(PopupDestination<TResult>)
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WithDestination_WhenNavigationFails_ReturnsFailure()
    {
        // Given
        var destination = new PopupDestination<TestPopupResult>("ConfirmPopup", new NavigationParameters());
        _popupService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(Result.Fail(ErrorCode.General, "boom"));

        // When
        var result = await _popupService.PresentAsync(destination);

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task PresentAsync_WithDestination_WhenCompletionResolves_ReturnsSuccessWithValue()
    {
        // Given
        var destination = new PopupDestination<TestPopupResult>("ConfirmPopup", new NavigationParameters());
        var expected = new TestPopupResult(true);

        _popupService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                CompleteWith(callInfo.Arg<INavigationParameters>(), expected);
                return Result.Ok();
            });

        // When
        var result = await _popupService.PresentAsync(destination);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public async Task PresentAsync_WithDestination_WhenCancelled_ReturnsCancelledFailure()
    {
        // Given
        var destination = new PopupDestination<TestPopupResult>("ConfirmPopup", new NavigationParameters());

        _popupService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                if (callInfo.Arg<INavigationParameters>()
                    ?.TryGetValue<TaskCompletionSource<TestPopupResult>>(
                        NavigationHints.PopupCompletionParam, out var tcs) == true)
                {
                    tcs.SetCanceled();
                }
                return Result.Ok();
            });
        _popupService.DismissAsync(Arg.Any<string>(), Arg.Any<bool>()).Returns(Result.Ok());

        // When
        var result = await _popupService.PresentAsync(destination);

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.Cancelled));
    }

    [Test]
    public async Task PresentAsync_WithDestination_WhenCancelled_DismissesThePopup()
    {
        // Given
        var destination = new PopupDestination<TestPopupResult>("ConfirmPopup", new NavigationParameters());

        _popupService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                if (callInfo.Arg<INavigationParameters>()
                    ?.TryGetValue<TaskCompletionSource<TestPopupResult>>(
                        NavigationHints.PopupCompletionParam, out var tcs) == true)
                {
                    tcs!.SetCanceled();
                }
                return Result.Ok();
            });
        _popupService.DismissAsync(Arg.Any<string>(), Arg.Any<bool>()).Returns(Result.Ok());

        // When
        await _popupService.PresentAsync(destination);

        // Then
        await _popupService.Received(1).DismissAsync("ConfirmPopup", Arg.Any<bool>());
    }

    // -----------------------------------------------------------------------
    // PresentAsync<TViewModel, TResult>()
    // -----------------------------------------------------------------------

    [Test]
    public async Task PresentAsync_WithViewModelType_UsesPopupNamingConvention()
    {
        // Given
        _popupService
            .PresentAsync(Arg.Any<string>(), Arg.Any<INavigationParameters>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                CompleteWith(callInfo.Arg<INavigationParameters>(), new TestPopupResult(false));
                return Result.Ok();
            });

        // When
        await _popupService.PresentAsync<TestPopupViewModel, TestPopupResult>();

        // Then
        await _popupService.Received(1).PresentAsync(
            "TestPopupPopup",
            Arg.Any<INavigationParameters>(),
            Arg.Any<bool>());
    }

    // -----------------------------------------------------------------------
    // DismissAsync<TViewModel>()
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissAsync_WithViewModelType_UsesPopupNamingConvention()
    {
        // Given
        _popupService.DismissAsync(Arg.Any<string>(), Arg.Any<bool>()).Returns(Result.Ok());

        // When
        await _popupService.DismissAsync<TestPopupViewModel>();

        // Then
        await _popupService.Received(1).DismissAsync("TestPopupPopup", Arg.Any<bool>());
    }
}