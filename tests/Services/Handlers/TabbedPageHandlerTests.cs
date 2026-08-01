using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Handlers;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Handlers;

[TestFixture]
public class TabbedPageHandlerTests
{
    private IPageNavigationHandler _sut = null!;
    private IPageFactory _pageFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new TabbedPageHandler(NullLogger.Instance);
        _pageFactory = Substitute.For<IPageFactory>();
        DispatcherProvider.SetCurrent(new TestDispatcherProvider());
    }

    private static NavigationParameters TabSwitchParameters()
    {
        var parameters = new NavigationParameters { { NavigationHints.IsTabbedPageSwitch, true } };
        return parameters;
    }

    // -----------------------------------------------------------------------
    // Explicit tab switch
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_WhenSwitchingToTabWrappingMatchingRootPage_SelectsThatTab()
    {
        // Given
        var targetTab = new NavigationPage(new FakePage());
        var otherTab = new FakeSecondPage();
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(targetTab);
        tabbedPage.Children.Add(otherTab);

        var pageInfo = new PageInfo(typeof(FakePage));
        var request = new NavigationRequest([pageInfo], TabSwitchParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(tabbedPage, request, animated: true);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tabbedPage.CurrentPage, Is.SameAs(targetTab));
    }

    [Test]
    public async Task HandleAsync_WhenSwitchingToUnwrappedTabMatchingPageType_SelectsThatTab()
    {
        // Given — a tab that is the page itself (not wrapped in a NavigationPage)
        var targetTab = new FakePage();
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(targetTab);

        var pageInfo = new PageInfo(typeof(FakePage));
        var request = new NavigationRequest([pageInfo], TabSwitchParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(tabbedPage, request, animated: true);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(tabbedPage.CurrentPage, Is.SameAs(targetTab));
    }

    [Test]
    public async Task HandleAsync_WhenSwitchingToTabNotRegistered_ReturnsNotSupportedFailure()
    {
        // Given — only a FakePage tab exists, but the request targets FakeSecondPage
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(new FakePage());

        var pageInfo = new PageInfo(typeof(FakeSecondPage));
        var request = new NavigationRequest([pageInfo], TabSwitchParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(tabbedPage, request, animated: true);

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    // -----------------------------------------------------------------------
    // Relative navigation within the current tab
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_WhenCurrentTabIsNotNavigationPage_ReturnsNotSupportedFailure()
    {
        // Given — current tab is a plain Page, not wrapped in a NavigationPage
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(new Page());

        var request = new NavigationRequest([], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(tabbedPage, request, animated: true);

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    [Test]
    public async Task HandleAsync_WhenCurrentTabIsNavigationPage_PushesRequestedPage()
    {
        // Given
        var navigationPage = new NavigationPage(new Page());
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(navigationPage);

        var pushedPage = new Page();
        var pageInfo = new PageInfo(typeof(FakePage));
        _pageFactory.CreatePage(pageInfo, Arg.Any<INavigationParameters>()).Returns(pushedPage);

        var request = new NavigationRequest([pageInfo], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(tabbedPage, request, animated: false);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.CurrentPage, Is.SameAs(pushedPage));
    }

    [Test]
    public async Task HandleAsync_WhenCurrentTabIsNavigationPage_ReturnsCompletedContext()
    {
        // Given
        var navigationPage = new NavigationPage(new Page());
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(navigationPage);

        var pageInfo = new PageInfo(typeof(FakePage));
        _pageFactory.CreatePage(pageInfo, Arg.Any<INavigationParameters>()).Returns(new Page());

        var request = new NavigationRequest([pageInfo], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(tabbedPage, request, animated: false);

        // Then
        Assert.That(result.TryGetValue(out var context), Is.True);
        Assert.That(context!.Action, Is.EqualTo(NavigationAction.Completed));
    }
}