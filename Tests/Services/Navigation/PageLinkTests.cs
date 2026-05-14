using Nkraft.MvvmEssentials.Extensions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Navigation;

[TestFixture]
public class PageLinkTests
{
    private INavigationService _navigationService = null!;

    [SetUp]
    public void SetUp() => _navigationService = Substitute.For<INavigationService>();

    // -----------------------------------------------------------------------
    // Absolute (withNavigation: false)
    // -----------------------------------------------------------------------

    [Test]
    public void Absolute_WithoutNavigation_SinglePage_BuildsDoubleSlashPath()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>();

        // Then
        Assert.That(link.FullPath, Is.EqualTo("//FakePage"));
    }

    [Test]
    public void Absolute_WithoutNavigation_MultiplePages_BuildsSlashDelimitedPath()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>()
            .Push<FakeSecondViewModel>();

        // Then
        Assert.That(link.FullPath, Is.EqualTo("//FakePage/FakeSecondPage"));
    }

    // -----------------------------------------------------------------------
    // Absolute (withNavigation: true)
    // -----------------------------------------------------------------------

    [Test]
    public void Absolute_WithNavigation_SinglePage_IncludesNavigationPageSegment()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: true)
            .Push<FakeViewModel>();

        // Then
        Assert.That(link.FullPath, Is.EqualTo("/NavigationPage/FakePage"));
    }

    [Test]
    public void Absolute_WithNavigation_MultiplePages_BuildsCorrectPath()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: true)
            .Push<FakeViewModel>()
            .Push<FakeSecondViewModel>();

        // Then
        Assert.That(link.FullPath, Is.EqualTo("/NavigationPage/FakePage/FakeSecondPage"));
    }

    // -----------------------------------------------------------------------
    // Relative
    // -----------------------------------------------------------------------

    [Test]
    public void Relative_WithoutNavigation_SinglePage_BuildsPlainPageName()
    {
        // When
        var link = _navigationService
            .Relative(withNavigation: false)
            .Push<FakeViewModel>();

        // Then
        Assert.That(link.FullPath, Is.EqualTo("FakePage"));
    }

    [Test]
    public void Relative_WithNavigation_SinglePage_IncludesNavigationPagePrefix()
    {
        // When
        var link = _navigationService
            .Relative(withNavigation: true)
            .Push<FakeViewModel>();

        // Then
        Assert.That(link.FullPath, Is.EqualTo("NavigationPage/FakePage"));
    }

    // -----------------------------------------------------------------------
    // Query-string parameters
    // -----------------------------------------------------------------------

    [Test]
    public void Push_WithPrimitiveParameters_AppendsQueryString()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>(new { Id = 7, Name = "Alice" });

        // Then
        Assert.That(link.FullPath, Does.Contain("FakePage?"));
        Assert.That(link.FullPath, Does.Contain("Id=7"));
        Assert.That(link.FullPath, Does.Contain("Name=Alice"));
    }

    [Test]
    public void Push_WithNullParameters_DoesNotAppendQueryString()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>(null);

        // Then
        Assert.That(link.FullPath, Does.Not.Contain("?"));
    }
}
