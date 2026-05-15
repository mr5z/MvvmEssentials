using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Extensions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Extensions;

[TestFixture]
public class NavigationExtensionTests
{
    private record LoginParameters(string ErrorMessage, int Test);

    private INavigationService _navigationService = null!;

    [SetUp]
    public void SetUp()
    {
        _navigationService = Substitute.For<INavigationService>();
        _navigationService
            .NavigateAsync(Arg.Any<string>(), Arg.Any<INavigationParameters?>(), Arg.Any<bool>())
            .Returns(Result.Ok());
    }

    // -----------------------------------------------------------------------
    // NavigateAsync<TViewModel>() — contextual, no parameters
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithViewModelOnly_CallsServiceWithCorrectPageName()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel>();

        // Then — "FakeViewModel" → "FakePage", no leading slash (contextual)
        await _navigationService.Received(1).NavigateAsync(
            "FakePage",
            Arg.Any<INavigationParameters?>(),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithViewModelOnly_DoesNotPassLeadingSlash()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel>();

        // Then — contextual nav must NOT have "/" prefix
        await _navigationService.Received(1).NavigateAsync(
            Arg.Is<string>(s => s != null && !s.StartsWith('/')),
            Arg.Any<INavigationParameters?>(),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithViewModelOnly_DefaultsAnimatedToTrue()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel>();

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Any<INavigationParameters?>(),
            true);
    }

    [Test]
    public async Task NavigateAsync_WithViewModelOnly_WhenAnimatedFalse_ForwardsFlag()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel>(animated: false);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Any<INavigationParameters?>(),
            false);
    }

    // -----------------------------------------------------------------------
    // NavigateAsync<TViewModel>(INavigationParameters) — contextual with params
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithINavigationParameters_PassesParametersThrough()
    {
        // Given
        var parameters = new NavigationParameters();
        parameters.Add("Key", "Value");

        // When
        await _navigationService.NavigateAsync<FakeViewModel>(parameters);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            "FakePage",
            parameters,
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithNullINavigationParameters_PassesNullThrough()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel>((INavigationParameters?)null);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            "FakePage",
            null,
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithINavigationParameters_AnimatedFalse_ForwardsFlag()
    {
        // Given
        var parameters = new NavigationParameters();

        // When
        await _navigationService.NavigateAsync<FakeViewModel>(parameters, animated: false);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Any<INavigationParameters?>(),
            false);
    }

    // -----------------------------------------------------------------------
    // NavigateAsync<TViewModel, TParameter> — contextual with typed object
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithTypedParameters_CallsServiceWithCorrectPageName()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel, object>(new { Name = "Alice" });

        // Then
        await _navigationService.Received(1).NavigateAsync(
            "FakePage",
            Arg.Any<INavigationParameters?>(),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithTypedParameters_MapsPropertiesToNavigationParameters()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel, object>(new { Name = "Alice", Age = 30 });

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Is<INavigationParameters?>(p =>
                p != null &&
                p.ContainsKey("Name") &&
                p.ContainsKey("Age")),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithTypedParameters_MapsCorrectValues()
    {
        // Given
        INavigationParameters? captured = null;
        _navigationService
            .NavigateAsync(Arg.Any<string>(), Arg.Any<INavigationParameters?>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<INavigationParameters?>();
                return Result.Ok();
            });

        // When
        await _navigationService.NavigateAsync<FakeViewModel, object>(new { Score = 99 });

        // Then
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.TryGetValue<int>("Score", out var score), Is.True);
        Assert.That(score, Is.EqualTo(99));
    }

    [Test]
    public async Task NavigateAsync_WithNullTypedParameters_PassesEmptyNavigationParameters()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel, object>(null);

        // Then — null object → empty (not null) INavigationParameters
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Is<INavigationParameters?>(p => p != null && p.IsEmpty),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithTypedParameters_AnimatedFalse_ForwardsFlag()
    {
        // When
        await _navigationService.NavigateAsync<FakeViewModel, object>(new { X = 1 }, animated: false);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Any<INavigationParameters?>(),
            false);
    }

    [Test]
    public async Task NavigateAsync_WithCustomRecordParameters_MapsCorrectly()
    {
        // Given
        INavigationParameters? captured = null;
        _navigationService
            .NavigateAsync(Arg.Any<string>(), Arg.Any<INavigationParameters?>(), Arg.Any<bool>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<INavigationParameters?>();
                return Result.Ok();
            });

        var parameters = new LoginParameters("Session expired", 42);

        // When
        await _navigationService.NavigateAsync<FakeViewModel, LoginParameters>(parameters);

        // Then
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.TryGetValue<string>("ErrorMessage", out var msg), Is.True);
        Assert.That(msg, Is.EqualTo("Session expired"));
        Assert.That(captured.TryGetValue<int>("Test", out var test), Is.True);
        Assert.That(test, Is.EqualTo(42));
    }
}