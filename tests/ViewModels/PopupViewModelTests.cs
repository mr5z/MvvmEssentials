using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Popups;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

[TestFixture]
public class PopupViewModelTests
{
    private IPopupService _popupService = null!;
    private TestPopupViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _popupService = Substitute.For<IPopupService>();
        _sut = new TestPopupViewModel(_popupService);
    }

    // -----------------------------------------------------------------------
    // PageName
    // -----------------------------------------------------------------------

    [Test]
    public void PageName_ReturnsPopupSuffix_NotPageSuffix()
    {
        // TestPopupViewModel → "TestPopupPopup"
        Assert.That(_sut.PageName, Is.EqualTo("TestPopupPopup"));
    }

    // -----------------------------------------------------------------------
    // Default dismiss-on behaviour
    // -----------------------------------------------------------------------

    [Test]
    public void ShouldDismissOnBackButtonPressed_DefaultsToTrue()
    {
        Assert.That(_sut.PublicShouldDismissOnBackButtonPressed, Is.True);
    }

    [Test]
    public void ShouldDismissOnBackgroundTapped_DefaultsToTrue()
    {
        Assert.That(_sut.PublicShouldDismissOnBackgroundTapped, Is.True);
    }

    // -----------------------------------------------------------------------
    // Dismiss with result
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissWithResult_WhenServiceSucceeds_CompletesTask()
    {
        // Given
        var tcs = new TaskCompletionSource<TestPopupResult>();
        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.PopupCompletionParam, tcs);
        ((IParameterSetAware)_sut).OnParametersSet(parameters);

        _popupService.DismissAsync().Returns(Result.Ok());

        // When
        var result = new TestPopupResult(true);
        await _sut.PublicDismissWithResult(result);

        // Then
        Assert.That(tcs.Task.IsCompletedSuccessfully, Is.True);
        Assert.That(await tcs.Task, Is.EqualTo(result));
    }

    [Test]
    public async Task DismissWithResult_WhenServiceFails_SetsExceptionOnCompletion()
    {
        // Given
        var tcs = new TaskCompletionSource<TestPopupResult>();
        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.PopupCompletionParam, tcs);
        ((IParameterSetAware)_sut).OnParametersSet(parameters);

        _popupService.DismissAsync().Returns(Result.Fail(ErrorCode.General, "error"));

        // When
        await _sut.PublicDismissWithResult(new TestPopupResult(false));

        // Then
        Assert.That(tcs.Task.IsFaulted, Is.True);
    }

    // -----------------------------------------------------------------------
    // DismissCommand
    // -----------------------------------------------------------------------

    [Test]
    public async Task DismissCommand_WhenServiceSucceeds_CancelsCompletion()
    {
        // Given
        var tcs = new TaskCompletionSource<TestPopupResult>();
        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.PopupCompletionParam, tcs);
        ((IParameterSetAware)_sut).OnParametersSet(parameters);

        _popupService.DismissAsync().Returns(Result.Ok());

        // When
        await _sut.DismissCommand.ExecuteAsync(null);

        // Then
        Assert.That(tcs.Task.IsCanceled, Is.True);
    }

    [Test]
    public async Task DismissCommand_WhenServiceFails_DoesNotCancelCompletion()
    {
        // Given
        var tcs = new TaskCompletionSource<TestPopupResult>();
        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.PopupCompletionParam, tcs);
        ((IParameterSetAware)_sut).OnParametersSet(parameters);

        _popupService.DismissAsync().Returns(Result.Fail(ErrorCode.General, "dismiss failed"));

        // When
        await _sut.DismissCommand.ExecuteAsync(null);

        // Then — tcs untouched, still pending
        Assert.That(tcs.Task.IsCompleted, Is.False);
    }

    // -----------------------------------------------------------------------
    // NotifyCancellation
    // -----------------------------------------------------------------------

    [Test]
    public void NotifyCancellation_WhenCompletionSet_CancelsTask()
    {
        // Given
        var tcs = new TaskCompletionSource<TestPopupResult>();
        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.PopupCompletionParam, tcs);
        ((IParameterSetAware)_sut).OnParametersSet(parameters);

        // When
        ((IPopupDismissible)_sut).NotifyCancellation();

        // Then
        Assert.That(tcs.Task.IsCanceled, Is.True);
    }

    [Test]
    public void NotifyCancellation_WhenNoCompletionSet_DoesNotThrow()
    {
        // Then
        Assert.DoesNotThrow(() => ((IPopupDismissible)_sut).NotifyCancellation());
    }
}
