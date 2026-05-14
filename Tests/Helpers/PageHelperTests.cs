// Requires [assembly: InternalsVisibleTo("Nkraft.MvvmEssentials.Tests")]
// in the main project.

using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Helpers;

[TestFixture]
public class PageHelperTests
{
    [Test]
    public void ToPageName_WithPagePattern_ReplacesViewModelSuffix()
    {
        // When
        var result = PageHelper.ToPageName<FakeViewModel>("Page");

        // Then
        Assert.That(result, Is.EqualTo("FakePage"));
    }

    [Test]
    public void ToPageName_WithPagePattern_HandlesMultiWordViewModelName()
    {
        // When
        var result = PageHelper.ToPageName<FakeSecondViewModel>("Page");

        // Then
        Assert.That(result, Is.EqualTo("FakeSecondPage"));
    }

    [Test]
    public void ToPageName_WithPopupPattern_ReplacesViewModelSuffix()
    {
        // When
        var result = PageHelper.ToPageName<FakeViewModel>("Popup");

        // Then
        Assert.That(result, Is.EqualTo("FakePopup"));
    }

    [Test]
    public void ToPageName_WithArbitrarySuffix_ReplacesViewModelSuffix()
    {
        // When
        var result = PageHelper.ToPageName<FakeViewModel>("View");

        // Then
        Assert.That(result, Is.EqualTo("FakeView"));
    }
}
