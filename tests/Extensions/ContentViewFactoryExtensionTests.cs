using Nkraft.MvvmEssentials.Services;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Extensions;

[TestFixture]
public class ContentViewFactoryExtensionTests
{
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
    }

    [Test]
    public void AddContentViewFactory_RegistersContentViewFactoryAsTransient()
    {
        // When
        _services.AddContentViewFactory();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IContentViewFactory));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
    }

    [Test]
    public void AddContentViewFactory_RegistersConcreteContentViewFactoryImplementation()
    {
        // When
        _services.AddContentViewFactory();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IContentViewFactory));
        Assert.That(descriptor!.ImplementationType, Is.EqualTo(typeof(ContentViewFactory)));
    }
}