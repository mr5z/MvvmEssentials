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
    public void MapView_RegistersViewModelAsScopedAndResolvesViewType()
    {
        // When
        _registry.MapView<MappedContentView, FakeViewModel>();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(FakeViewModel));
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
        Assert.That(_registry.ResolveViewType(typeof(FakeViewModel)), Is.EqualTo(typeof(MappedContentView)));
    }

    [Test]
    public void MapView_WhenViewModelMappedTwice_ThrowsInvalidOperationException()
    {
        // Given
        _registry.MapView<MappedContentView, FakeViewModel>();

        // Then
        Assert.Throws<InvalidOperationException>(() => _registry.MapView<MappedContentView, FakeViewModel>());
    }
}
