using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.ContentViews;

internal sealed class FakeContentView : ContentView;

[TestFixture]
public class ContentViewFactoryTests
{
    private ServiceProvider _serviceProvider = null!;
    private IContentViewFactory _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        IPageRegistry registry = new PageRegistry(services);
        registry.MapView<FakeContentView, FakeViewModel>(); // also registers FakeViewModel as scoped
        _serviceProvider = services.BuildServiceProvider();
        _sut = new ContentViewFactory(_serviceProvider, (IPageCatalog)registry);
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
    }

    [Test]
    public void CreateView_ReturnsInstanceOfRequestedContentViewType()
    {
        // When
        var view = _sut.CreateView<FakeViewModel>();

        // Then
        Assert.That(view, Is.InstanceOf<FakeContentView>());
    }

    [Test]
    public void CreateView_SetsBindingContextToResolvedViewModel()
    {
        // When
        var view = _sut.CreateView<FakeViewModel>();

        // Then
        Assert.That(view.BindingContext, Is.InstanceOf<FakeViewModel>());
    }

    [Test]
    public void CreateView_MultipleCalls_ProduceDistinctViewModelInstances()
    {
        // Given — each CreateView call opens its own DI scope
        var first = _sut.CreateView<FakeViewModel>();

        // When
        var second = _sut.CreateView<FakeViewModel>();

        // Then
        Assert.That(second.BindingContext, Is.Not.SameAs(first.BindingContext));
    }

    [Test]
    public void CreateView_WhenViewModelUnmapped_ThrowsWithMapViewHint()
    {
        // Given
        var services = new ServiceCollection();
        using var provider = services.BuildServiceProvider();
        IContentViewFactory sut = new ContentViewFactory(provider, new PageRegistry(services));

        // Then
        var ex = Assert.Throws<InvalidOperationException>(() => sut.CreateView<FakeViewModel>());
        Assert.That(ex!.Message, Does.Contain("MapView"));
    }

    [Test]
    public void Dispose_CompletesWithoutThrowingWhenViewsWereCreated()
    {
        // Given
        _sut.CreateView<FakeViewModel>();

        // Then
        Assert.DoesNotThrow(() => _sut.Dispose());
    }

    [Test]
    public void Dispose_CompletesWithoutThrowingWhenNoViewsWereCreated()
    {
        // Then
        Assert.DoesNotThrow(() => _sut.Dispose());
    }
}