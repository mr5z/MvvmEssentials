using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

[TestFixture]
public class PopupDestinationTests
{
    [Test]
    public void Constructor_SetsPopupName()
    {
        // Given
        var parameters = new NavigationParameters();

        // When
        var destination = new PopupDestination<bool>("ConfirmPopup", parameters);

        // Then
        Assert.That(destination.PopupName, Is.EqualTo("ConfirmPopup"));
    }

    [Test]
    public void Constructor_SetsParameters()
    {
        // Given
        var parameters = new NavigationParameters();

        // When
        var destination = new PopupDestination<bool>("ConfirmPopup", parameters);

        // Then
        Assert.That(destination.Parameters, Is.SameAs(parameters));
    }
}