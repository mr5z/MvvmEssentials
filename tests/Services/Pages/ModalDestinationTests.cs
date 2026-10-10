using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

[TestFixture]
public class ModalDestinationTests
{
    [Test]
    public void Constructor_SetsModalName()
    {
        // Given
        var parameters = new NavigationParameters();

        // When
        var destination = new ModalDestination<bool>("EditPage", parameters);

        // Then
        Assert.That(destination.ModalName, Is.EqualTo("EditPage"));
    }

    [Test]
    public void Constructor_SetsParameters()
    {
        // Given
        var parameters = new NavigationParameters();

        // When
        var destination = new ModalDestination<bool>("EditPage", parameters);

        // Then
        Assert.That(destination.Parameters, Is.SameAs(parameters));
    }
}