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
        // When
        _services.AddPageRegistry(_ => { });

        // Then
        var instance = _services.BuildServiceProvider().GetRequiredService<IPageRegistry>();
        Assert.That(instance, Is.Not.Null);
    }

    [Test]
    public void AddPageRegistry_RegistersTheConfiguredRegistryAsSingleton()
    {
        // Given
        IPageRegistry? configuredRegistry = null;

        // When
        _services.AddPageRegistry(registry => configuredRegistry = registry);

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IPageRegistry));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(descriptor.ImplementationInstance, Is.SameAs(configuredRegistry));
    }
}