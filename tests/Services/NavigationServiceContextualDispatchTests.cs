using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class NavigationServiceContextualDispatchTests
{
    private IPageFactory _pageFactory = null!;
    private IApplicationContext _applicationContext = null!;
    private INavigationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _pageFactory = Substitute.For<IPageFactory>();
        _applicationContext = Substitute.For<IApplicationContext>();

        _sut = new NavigationService(
            NullLogger<NavigationService>.Instance,
            _pageFactory,
            _applicationContext);

        DispatcherProvider.SetCurrent(new TestDispatcherProvider());

        var pushedPage = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(FakePage))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(pushedPage);
    }

    private void SetMainPage(Page page) => _applicationContext.MainPage.Returns(page);

    // -----------------------------------------------------------------------
    // Switch arms — one handler type per current-page type
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WhenCurrentPageIsNavigationPage_DispatchesToNavigationPageHandler()
    {
        // Given
        SetMainPage(new NavigationPage(new Page()));

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WhenCurrentPageIsTabbedPageWithNavigationTab_DispatchesToTabbedPageHandler()
    {
        // Given
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(new NavigationPage(new Page()));
        SetMainPage(tabbedPage);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WhenCurrentPageIsUnrecognizedType_DispatchesToUnsupportedPageHandler()
    {
        // Given — a plain ContentPage matches none of NavigationPage/TabbedPage/FlyoutPage,
        // so the switch's default arm applies on the very first loop iteration
        SetMainPage(new ContentPage());

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    // -----------------------------------------------------------------------
    // Loop continuation — FlyoutPageHandler is the only handler that returns
    // ContinueInto, which is what makes the while loop actually iterate again
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WhenCurrentPageIsFlyoutPageWithNavigationDetail_LoopsIntoDetailAndSucceeds()
    {
        // Given — FlyoutPageHandler returns ContinueInto(Detail) for a non-root request,
        // so the loop must run a second pass, this time dispatching to NavigationPageHandler
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new NavigationPage(new Page())
        };
        SetMainPage(flyoutPage);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WhenCurrentPageIsFlyoutPageWithUnsupportedDetail_LoopsIntoDetailThenFails()
    {
        // Given — Detail is a plain page, so the second pass resolves to
        // UnsupportedPageHandler; this hits both the "continue" branch (pass 1) and
        // the "return without continuing" branch (pass 2) in the same call
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page()
        };
        SetMainPage(flyoutPage);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }
}