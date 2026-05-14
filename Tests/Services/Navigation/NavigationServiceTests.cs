using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Navigation;

[TestFixture]
public class NavigationServiceTests
{
    private IPageFactory _pageFactory = null!;
    private IApplicationContext _applicationContext = null!;
    private INavigationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _pageFactory = Substitute.For<IPageFactory>();
        _applicationContext = Substitute.For<IApplicationContext>();

        // NullLogger avoids Castle DynamicProxy issues with ILogger<NavigationService>
        // where NavigationService is internal
        _sut = new NavigationService(
            NullLogger<NavigationService>.Instance,
            _pageFactory,
            _applicationContext);
    }

    // -----------------------------------------------------------------------
    // NavigateAsync — factory error paths
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WhenGetPageTypesThrows_ReturnsFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Throws(new InvalidOperationException("Page not found."));

        // When
        var result = await _sut.NavigateAsync("//UnknownPage");

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WhenPageListIsEmpty_ReturnsFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([]);

        // When
        var result = await _sut.NavigateAsync("//SomePage");

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // NavigateAsync — absolute single-page navigation
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithAbsolutePathAndSinglePage_SetsMainPage()
    {
        // Given
        var page = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(Page))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(page);

        // When
        var result = await _sut.NavigateAsync("//FakePage");

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_applicationContext.MainPage, Is.SameAs(page));
    }

    // -----------------------------------------------------------------------
    // NavigateAsync — relative path requires existing MainPage
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithRelativePath_WhenNoWindowExists_ReturnsFailure()
    {
        // Given
        var page = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(Page))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(page);

        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WithRelativePath_WhenMainPageIsPlainPage_ReturnsFailure()
    {
        // Given
        var page = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(Page))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(page);

        // Real Window with a plain Page — Window.Page is not virtual so we can't substitute it
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then — relative nav on a plain Page is not supported
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // NavigateBackAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateBackAsync_WhenNoWindowExists_ReturnsFailure()
    {
        // Given
        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateBackAsync_WhenMainPageIsPlainPage_ReturnsFailure()
    {
        // Given — real Window with plain Page; SendBackButtonPressed returns false in tests
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // NavigateToRootAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateToRootAsync_WhenNoWindowExists_ReturnsFailure()
    {
        // Given
        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateToRootAsync_WhenMainPageIsNotNavigationPage_ReturnsFailure()
    {
        // Given — real Window with plain Page, not a NavigationPage
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }
}
