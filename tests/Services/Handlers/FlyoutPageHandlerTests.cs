using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Handlers;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using Nkraft.MvvmEssentials.ViewModels;
using NSubstitute;
using NUnit.Framework;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Handlers;

// IInitialDetail is internal; a minimal test double lets us drive the "first navigation
// stores the original detail" branch without pulling in a real FlyoutViewModel<,>.
internal sealed class FakeInitialDetailHost : IInitialDetail
{
    public Page? DetailPage { get; set; }
}

[TestFixture]
public class FlyoutPageHandlerTests
{
    private IPageNavigationHandler _sut = null!;
    private IPageFactory _pageFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new FlyoutPageHandler(NullLogger.Instance);
        _pageFactory = Substitute.For<IPageFactory>();
    }

    private static NavigationParameters FlyoutDetailRootParameters()
    {
        var parameters = new NavigationParameters { { NavigationHints.IsFlyoutDetailRoot, true } };
        return parameters;
    }

    // -----------------------------------------------------------------------
    // No Detail set
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_WhenDetailIsNull_ReturnsInvalidStateFailure()
    {
        // Given
        var flyoutPage = new FlyoutPage { Flyout = new ContentPage { Title = "test" } };
        var request = new NavigationRequest([], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    // -----------------------------------------------------------------------
    // Non-flyout-root request — descend into Detail
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_WhenNotFlyoutDetailRootRequest_ReturnsContinueIntoDetail()
    {
        // Given
        var detail = new Page();
        var flyoutPage = new FlyoutPage { Flyout = new ContentPage { Title = "test" }, Detail = detail };
        var request = new NavigationRequest([], new NavigationParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.TryGetValue(out var context), Is.True);
        Assert.That(context!.Action, Is.EqualTo(NavigationAction.ContinueInto));
        Assert.That(context.NextPage, Is.SameAs(detail));
    }

    // -----------------------------------------------------------------------
    // Flyout-root request — BindingContext doesn't support tracking initial detail
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_FlyoutRoot_WhenBindingContextIsNotIInitialDetail_ReturnsInvalidStateFailure()
    {
        // Given
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = new Page(),
            BindingContext = new object()
        };
        var request = new NavigationRequest([new PageInfo(typeof(FakePage))], FlyoutDetailRootParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    // -----------------------------------------------------------------------
    // Flyout-root request — navigating back to the original detail
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_FlyoutRoot_WhenTargetMatchesInitialDetailViewModel_RestoresOriginalDetail()
    {
        // Given — original detail's BindingContext is FakeViewModel; the request targets
        // FakePage, whose corresponding ViewModel name ("FakeViewModel") matches
        var initialDetail = new Page { BindingContext = new FakeViewModel() };
        var flyoutHost = new FakeInitialDetailHost();
        var flyoutPage = new FlyoutPage
        {
            Title = "test",
            Flyout = new ContentPage { Title = "test" },
            Detail = initialDetail,
            BindingContext = flyoutHost
        };
        var request = new NavigationRequest([new PageInfo(typeof(FakePage))], FlyoutDetailRootParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(flyoutPage.Detail, Is.SameAs(initialDetail));
    }

    [Test]
    public async Task HandleAsync_FlyoutRoot_WhenTargetMatchesInitialDetailViewModel_ReturnsCompletedContext()
    {
        // Given
        var initialDetail = new Page { BindingContext = new FakeViewModel() };
        var flyoutHost = new FakeInitialDetailHost();
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "test" },
            Detail = initialDetail,
            BindingContext = flyoutHost
        };
        var request = new NavigationRequest([new PageInfo(typeof(FakePage))], FlyoutDetailRootParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.TryGetValue(out var context), Is.True);
        Assert.That(context!.Action, Is.EqualTo(NavigationAction.Completed));
    }

    // -----------------------------------------------------------------------
    // Flyout-root request — navigating to a different ViewModel, Detail is a plain page
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_FlyoutRoot_WhenTargetDiffersAndDetailIsPlainPage_WrapsTargetInNavigationPage()
    {
        // Given — initial detail is FakeViewModel-bound; request targets FakeSecondPage
        // ("FakeSecondViewModel"), which doesn't match, so it takes the "materialize" path
        var initialDetail = new Page { BindingContext = new FakeViewModel() };
        var flyoutHost = new FakeInitialDetailHost();
        var flyoutPage = new FlyoutPage
        {
            Title = "test",
            Flyout = new ContentPage { Title = "test" },
            Detail = initialDetail,
            BindingContext = flyoutHost
        };

        var targetPage = new Page();
        var pageInfo = new PageInfo(typeof(FakeSecondPage));
        _pageFactory.CreatePage(pageInfo, Arg.Any<INavigationParameters>()).Returns(targetPage);

        var request = new NavigationRequest([pageInfo], FlyoutDetailRootParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(flyoutPage.Detail, Is.InstanceOf<NavigationPage>());
        Assert.That(((NavigationPage)flyoutPage.Detail!).CurrentPage, Is.SameAs(targetPage));
    }

    // -----------------------------------------------------------------------
    // Flyout-root request — navigating to a different ViewModel, Detail already a NavigationPage
    // -----------------------------------------------------------------------

    [Test]
    public async Task HandleAsync_FlyoutRoot_WhenTargetDiffersAndDetailIsNavigationPage_ReplacesRootWithTarget()
    {
        // Given
        var originalRoot = new Page { BindingContext = new FakeViewModel() };
        var navigationDetail = new NavigationPage(originalRoot);
        var flyoutHost = new FakeInitialDetailHost();
        var flyoutPage = new FlyoutPage
        {
            Title = "test",
            Flyout = new ContentPage { Title = "test" },
            Detail = navigationDetail,
            BindingContext = flyoutHost
        };

        var targetPage = new Page();
        var pageInfo = new PageInfo(typeof(FakeSecondPage));
        _pageFactory.CreatePage(pageInfo, Arg.Any<INavigationParameters>()).Returns(targetPage);

        var request = new NavigationRequest([pageInfo], FlyoutDetailRootParameters(), _pageFactory);

        // When
        var result = await _sut.HandleAsync(flyoutPage, request, animated: true);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationDetail.CurrentPage, Is.SameAs(targetPage));
    }
}