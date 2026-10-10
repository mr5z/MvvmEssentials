using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

[TestFixture]
public class PageLeaseTests
{
    private IPageFactory _pageFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _pageFactory = Substitute.For<IPageFactory>();
    }

    private Page CreateVia(PageLease lease, Page page)
    {
        _pageFactory.CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters?>()).Returns(page);
        return lease.Create(new PageInfo(typeof(FakePage)), null);
    }

    [Test]
    public void Dispose_WhenNotCommitted_ReleasesEveryCreatedPage()
    {
        // Given
        var first = new FakePage();
        var second = new FakePage();
        var lease = new PageLease(_pageFactory);
        CreateVia(lease, first);
        CreateVia(lease, second);

        // When
        ((IDisposable)lease).Dispose();

        // Then
        _pageFactory.Received(1).ReleasePage(first);
        _pageFactory.Received(1).ReleasePage(second);
    }

    [Test]
    public void Dispose_WhenCommitted_DoesNotReleaseTheRoot()
    {
        // Given
        var page = new FakePage();
        var lease = new PageLease(_pageFactory);
        CreateVia(lease, page);
        lease.Commit(page);

        // When
        ((IDisposable)lease).Dispose();

        // Then
        _pageFactory.DidNotReceive().ReleasePage(Arg.Any<Page>());
    }

    [Test]
    public void Dispose_WhenCommitted_KeepsPagesReachableFromRootAndReleasesTheRest()
    {
        // Given
        var child = new ContentPage();
        var orphan = new FakePage();
        var lease = new PageLease(_pageFactory);
        CreateVia(lease, child);
        CreateVia(lease, orphan);
        var root = new NavigationPage(child);
        lease.Commit(root);

        // When
        ((IDisposable)lease).Dispose();

        // Then
        _pageFactory.DidNotReceive().ReleasePage(child);
        _pageFactory.Received(1).ReleasePage(orphan);
    }

    [Test]
    public void Dispose_WhenReleasePageThrows_DoesNotThrowAndReleasesRemainingPages()
    {
        // Given
        var failing = new FakePage();
        var other = new FakePage();
        _pageFactory.When(f => f.ReleasePage(failing)).Throw(new InvalidOperationException("boom"));
        var lease = new PageLease(_pageFactory);
        CreateVia(lease, failing);
        CreateVia(lease, other);

        // When / Then
        Assert.DoesNotThrow(((IDisposable)lease).Dispose);
        _pageFactory.Received(1).ReleasePage(other);
    }

    [Test]
    public void ReleaseUnreachable_WhenReleasePageThrows_ReturnsAggregateException()
    {
        // Given
        var page = new FakePage();
        _pageFactory.When(f => f.ReleasePage(page)).Throw(new InvalidOperationException("boom"));
        var lease = new PageLease(_pageFactory);
        CreateVia(lease, page);

        // When
        var ex = lease.ReleaseUnreachable(root: null);

        // Then
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.InnerExceptions, Has.Count.EqualTo(1));
    }

    [Test]
    public void Dispose_AfterReleaseUnreachable_DoesNotReleaseTwice()
    {
        // Given
        var page = new FakePage();
        var lease = new PageLease(_pageFactory);
        CreateVia(lease, page);
        lease.ReleaseUnreachable(root: null);

        // When
        ((IDisposable)lease).Dispose();

        // Then
        _pageFactory.Received(1).ReleasePage(page);
    }
}