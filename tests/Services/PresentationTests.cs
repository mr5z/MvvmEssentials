using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Presentations;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

/// <summary>
/// The rules Presentation applies for every presenter; popups and modals test their own on top.
/// </summary>
[TestFixture]
public class PresentationTests
{
    [Test]
    public async Task PushAsync_OnAPresenterWithoutAPageStack_FailsWithNotSupported()
    {
        // Given: a popup
        var presentation = new Presentation();
        presentation.Open(Substitute.For<IPresenter>(), new ContentPage(), new ContentPage());

        // When
        var pushed = await presentation.PushAsync("NextPage", null, animated: false);
        var popped = await presentation.PopAsync(animated: false);

        // Then
        Assert.That(pushed.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
        Assert.That(popped.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    [Test]
    public async Task CompleteAsync_WhenNobodyAwaitsAResult_OnlyDismisses()
    {
        // Given: PresentAsync(name)
        var presenter = Substitute.For<IPresenter>();
        var presentation = new Presentation();
        presentation.Open(presenter, new ContentPage(), new ContentPage());
        presenter.DismissAsync(presentation).Returns(Result.Ok());

        // When
        var result = await presentation.CompleteAsync("ignored");

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(presentation.IsClosed, Is.True);
    }

    [Test]
    public void OnClosed_Twice_KeepsTheFirstOutcome()
    {
        // Given
        var presentation = new Presentation<int>();
        presentation.SetPendingResult(1);

        // When: the presenter reports the removal, then DismissAsync returns and closes again
        presentation.OnClosed();
        presentation.OnClosed();

        // Then
        Assert.That(presentation.Result.Result, Is.EqualTo(1));
    }

    [Test]
    public void Operations_BeforeOpen_Throw()
    {
        // Given: a view model could only get a presentation from a presenting service, so this is a library bug
        var presentation = new Presentation();

        // Then
        Assert.ThrowsAsync<InvalidOperationException>(() => presentation.CancelAsync(() => Task.FromResult(true)));
    }
}
