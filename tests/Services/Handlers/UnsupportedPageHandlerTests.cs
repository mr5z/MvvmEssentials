using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Handlers;
using Nkraft.MvvmEssentials.Services.Pages;
using NSubstitute;
using NUnit.Framework;
using NavigationRequest = Nkraft.MvvmEssentials.Services.Pages.NavigationRequest;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Handlers;

[TestFixture]
public class UnsupportedPageHandlerTests
{
    private IPageNavigationHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new UnsupportedPageHandler(NullLogger.Instance);
    }

    [Test]
    public async Task HandleAsync_AlwaysReturnsFailure()
    {
        // Given
        var page = new Page();
        var request = new NavigationRequest([], new NavigationParameters(), Substitute.For<IPageFactory>());

        // When
        var result = await _sut.HandleAsync(page, request, animated: true);

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task HandleAsync_AlwaysReturnsNotSupportedErrorCode()
    {
        // Given
        var page = new Page();
        var request = new NavigationRequest([], new NavigationParameters(), Substitute.For<IPageFactory>());

        // When
        var result = await _sut.HandleAsync(page, request, animated: false);

        // Then
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }
}