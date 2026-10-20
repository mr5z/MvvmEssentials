using Nkraft.MvvmEssentials.Services;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Extensions;

[TestFixture]
public class PageRegistryCollectionExtensionTests
{
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
    }

    [Test]
    public void AddPageRegistry_InvokesConfigureCallbackWithNewRegistry()
    {
        // Given
        IPageRegistry? configuredRegistry = null;

        // When
        _services.AddPageRegistry(registry => configuredRegistry = registry);

        // Then
        Assert.That(configuredRegistry, Is.Not.Null);
    }

    [Test]
    public void AddPageRegistry_DoesNotRegisterIPageRegistry()
    {
        // When
        _services.AddPageRegistry(_ => { });

        // Then: the registry is configuration only; the library resolves IPageCatalog.
        Assert.That(_services.Any(d => d.ServiceType == typeof(IPageRegistry)), Is.False);
    }

    [Test]
    public void AddPageRegistry_RegistersTheSameRegistryAsPageCatalog()
    {
        // Given
        IPageRegistry? configuredRegistry = null;

        // When
        _services.AddPageRegistry(registry => configuredRegistry = registry);

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IPageCatalog));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(descriptor.ImplementationInstance, Is.SameAs(configuredRegistry));
    }
}