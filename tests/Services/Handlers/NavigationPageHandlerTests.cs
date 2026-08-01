using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Handlers;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Handlers;

[TestFixture]
public class NavigationPageHandlerTests
{
    private IPageNavigationHandler _sut = null!;
    private IPageFactory _pageFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new NavigationPageHandler(NullLogger.Instance);
        _pageFactory = Substitute.For<IPageFactory>();
    }

    // -----------------------------------------------------------------------
    // Empty request
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_WhenNoPagesToNavigate_ReturnsFailure()
    {
        // Given
        var navigationPage = new NavigationPage(new Page());
        var request = new NavigationRequest([], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(navigationPage, request, animated: true);

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }

    // -----------------------------------------------------------------------
    // Happy path
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_WithOnePageToNavigate_PushesItOntoNavigationPage()
    {
        // Given
        var navigationPage = new NavigationPage(new Page());
        var pushedPage = new Page();
        var pageInfo = new PageInfo(typeof(FakePage));

        _pageFactory.CreatePage(pageInfo, Arg.Any<INavigationParameters>()).Returns(pushedPage);

        var request = new NavigationRequest([pageInfo], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(navigationPage, request, animated: false);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.CurrentPage, Is.SameAs(pushedPage));
    }

    [Test]
    public async Task HandleAsync_WithMultiplePages_PushesEachOneInOrder()
    {
        // Given
        var navigationPage = new NavigationPage(new Page());
        var firstPage = new Page();
        var secondPage = new Page();
        var firstInfo = new PageInfo(typeof(FakePage));
        var secondInfo = new PageInfo(typeof(FakeSecondPage));

        _pageFactory.CreatePage(firstInfo, Arg.Any<INavigationParameters>()).Returns(firstPage);
        _pageFactory.CreatePage(secondInfo, Arg.Any<INavigationParameters>()).Returns(secondPage);

        var request = new NavigationRequest([firstInfo, secondInfo], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(navigationPage, request, animated: false);

        // Then — last pushed page ends up on top of the stack
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.CurrentPage, Is.SameAs(secondPage));
        Assert.That(navigationPage.Navigation.NavigationStack, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task HandleAsync_WithPagesToNavigate_ReturnsCompletedNavigationContext()
    {
        // Given
        var navigationPage = new NavigationPage(new Page());
        var pageInfo = new PageInfo(typeof(FakePage));
        _pageFactory.CreatePage(pageInfo, Arg.Any<INavigationParameters>()).Returns(new Page());
        var request = new NavigationRequest([pageInfo], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(navigationPage, request, animated: true);

        // Then
        Assert.That(result.TryGetValue(out var context), Is.True);
        Assert.That(context!.Action, Is.EqualTo(NavigationAction.Completed));
    }
}