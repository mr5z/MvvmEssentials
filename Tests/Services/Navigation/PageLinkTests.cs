using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Extensions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Navigation;

[TestFixture]
public class PageLinkTests
{
    private record PageParams(int Id, string Name);

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
    // Push<TViewModel> — anonymous object query string
    // -----------------------------------------------------------------------

    [Test]
    public void Push_WithAnonymousParameters_AppendsQueryString()
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
    public void Push_WithNullAnonymousParameters_DoesNotAppendQueryString()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>(null);

        // Then
        Assert.That(link.FullPath, Does.Not.Contain("?"));
    }

    // -----------------------------------------------------------------------
    // Push<TViewModel, TParameter> — strongly-typed object query string
    // -----------------------------------------------------------------------

    [Test]
    public void Push_WithStronglyTypedParameters_AppendsQueryString()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel, PageParams>(new PageParams(7, "Alice"));

        // Then
        Assert.That(link.FullPath, Does.Contain("FakePage?"));
        Assert.That(link.FullPath, Does.Contain("Id=7"));
        Assert.That(link.FullPath, Does.Contain("Name=Alice"));
    }

    [Test]
    public void Push_StronglyTypedAndAnonymous_ProduceSameQueryString()
    {
        // Given
        var typed = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel, PageParams>(new PageParams(7, "Alice"));

        var anonymous = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>(new { Id = 7, Name = "Alice" });

        // Then
        Assert.That(typed.FullPath, Is.EqualTo(anonymous.FullPath));
    }

    [Test]
    public void Push_WithNullStronglyTypedParameters_DoesNotAppendQueryString()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel, PageParams>(null);

        // Then
        Assert.That(link.FullPath, Does.Not.Contain("?"));
    }

    // -----------------------------------------------------------------------
    // Multi-segment with parameters on each segment
    // -----------------------------------------------------------------------

    [Test]
    public void Push_MultipleSegmentsEachWithParameters_BuildsCorrectPath()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: true)
            .Push<FakeViewModel>(new { A = 1 })
            .Push<FakeSecondViewModel>(new { B = 2 });

        // Then
        Assert.That(link.FullPath, Does.Contain("FakePage?A=1"));
        Assert.That(link.FullPath, Does.Contain("FakeSecondPage?B=2"));
    }

    [Test]
    public void Push_MultipleSegmentsWithParameters_MaintainsSegmentOrder()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: true)
            .Push<FakeViewModel>(new { A = 1 })
            .Push<FakeSecondViewModel>(new { B = 2 });

        // Then — FakePage must appear before FakeSecondPage
        var fakeIndex = link.FullPath.IndexOf("FakePage", StringComparison.Ordinal);
        var secondIndex = link.FullPath.IndexOf("FakeSecondPage", StringComparison.Ordinal);
        Assert.That(fakeIndex, Is.LessThan(secondIndex));
    }

    // -----------------------------------------------------------------------
    // PageLink.NavigateAsync() — delegates to INavigationService
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_CallsServiceWithBuiltFullPath()
    {
        // Given
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>();

        // When
        await link.NavigateAsync();

        // Then
        await _navigationService.Received(1).NavigateAsync(
            "//FakePage",
            Arg.Any<INavigationParameters?>(),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_WithMultiSegmentPath_CallsServiceWithFullPath()
    {
        // Given
        var link = _navigationService
            .Absolute(withNavigation: true)
            .Push<FakeViewModel>()
            .Push<FakeSecondViewModel>();

        // When
        await link.NavigateAsync();

        // Then
        await _navigationService.Received(1).NavigateAsync(
            "/NavigationPage/FakePage/FakeSecondPage",
            Arg.Any<INavigationParameters?>(),
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_PassesINavigationParametersToService()
    {
        // Given
        var parameters = new NavigationParameters();
        parameters.Add("Token", "abc123");

        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>();

        // When
        await link.NavigateAsync(parameters);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            parameters,
            Arg.Any<bool>());
    }

    [Test]
    public async Task NavigateAsync_DefaultsAnimatedToTrue()
    {
        // Given
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>();

        // When
        await link.NavigateAsync();

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Any<INavigationParameters?>(),
            true);
    }

    [Test]
    public async Task NavigateAsync_WhenAnimatedFalse_ForwardsFlagToService()
    {
        // Given
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>();

        // When
        await link.NavigateAsync(animated: false);

        // Then
        await _navigationService.Received(1).NavigateAsync(
            Arg.Any<string>(),
            Arg.Any<INavigationParameters?>(),
            false);
    }

    [Test]
    public async Task NavigateAsync_ReturnsServiceResult()
    {
        // Given
        _navigationService
            .NavigateAsync(Arg.Any<string>(), Arg.Any<INavigationParameters?>(), Arg.Any<bool>())
            .Returns(Result.Fail(ErrorCode.General, "fail"));

        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>();

        // When
        var result = await link.NavigateAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // Tab selection via SelectedTabIndex parameter
    // -----------------------------------------------------------------------

    [Test]
    public void Push_WithSelectedTabIndex_AppendsToQueryString()
    {
        // When
        var link = _navigationService
            .Absolute(withNavigation: false)
            .Push<FakeViewModel>(new { SelectedTabIndex = 2 });

        // Then
        Assert.That(link.FullPath, Does.Contain("SelectedTabIndex=2"));
    }
}