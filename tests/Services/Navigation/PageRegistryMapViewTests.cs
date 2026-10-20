using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Navigation;

internal sealed class MappedContentView : ContentView;

[TestFixture]
public class PageRegistryMapViewTests
{
    private ServiceCollection _services = null!;
    private IPageRegistry _registry = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
        _registry = new PageRegistry(_services);
    }

    [Test]
    public void MapView_RegistersViewModelAsScoped()
    {
        // When
        _registry.MapView<MappedContentView, FakeViewModel>();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(FakeViewModel));
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void MapView_WhenViewModelMappedTwice_ThrowsInvalidOperationException()
    {
        // Given
        _registry.MapView<MappedContentView, FakeViewModel>();

        // Then
        Assert.Throws<InvalidOperationException>(() => _registry.MapView<MappedContentView, FakeViewModel>());
    }

    [Test]
    public void CreateView_WhenViewModelMapped_ReturnsNewViewInstance()
    {
        // Given
        _registry.MapView<MappedContentView, FakeViewModel>();

        // When
        var view = ((IPageCatalog)_registry).CreateView(typeof(FakeViewModel));

        // Then
        Assert.That(view, Is.TypeOf<MappedContentView>());
    }

    [Test]
    public void CreateView_WhenViewModelNotMapped_ReturnsNull()
    {
        // Then
        Assert.That(((IPageCatalog)_registry).CreateView(typeof(FakeViewModel)), Is.Null);
    }
}
