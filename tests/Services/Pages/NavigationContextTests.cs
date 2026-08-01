using Nkraft.MvvmEssentials.Services.Pages;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

[TestFixture]
public class NavigationContextTests
{
    [Test]
    public void Complete_ReturnsContextWithCompletedAction()
    {
        // When
        var context = NavigationContext.Complete();

        // Then
        Assert.That(context.Action, Is.EqualTo(NavigationAction.Completed));
    }

    [Test]
    public void Complete_ReturnsContextWithNullNextPage()
    {
        // When
        var context = NavigationContext.Complete();

        // Then
        Assert.That(context.NextPage, Is.Null);
    }

    [Test]
    public void Into_ReturnsContextWithContinueIntoAction()
    {
        // Given
        var page = new Page();

        // When
        var context = NavigationContext.Into(page);

        // Then
        Assert.That(context.Action, Is.EqualTo(NavigationAction.ContinueInto));
    }

    [Test]
    public void Into_ReturnsContextWithGivenNextPage()
    {
        // Given
        var page = new Page();

        // When
        var context = NavigationContext.Into(page);

        // Then
        Assert.That(context.NextPage, Is.SameAs(page));
    }
}