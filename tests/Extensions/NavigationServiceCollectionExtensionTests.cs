using Mopups.Interfaces;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Extensions;

[TestFixture]
public class NavigationServiceCollectionExtensionTests
{
    private ServiceCollection _services = null!;

    [SetUp]
    public void SetUp()
    {
        _services = new ServiceCollection();
    }

    [Test]
    public void AddNavigationService_RegistersPageFactoryAsSingleton()
    {
        // When
        _services.AddNavigationService();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IPageFactory));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(descriptor.ImplementationType, Is.EqualTo(typeof(PageFactory)));
    }

    [Test]
    public void AddNavigationService_RegistersNavigationServiceAsSingleton()
    {
        // When
        _services.AddNavigationService();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(INavigationService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void AddNavigationService_RegistersPopupServiceAsSingleton()
    {
        // When
        _services.AddNavigationService();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IPopupService));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void AddNavigationService_RegistersApplicationContextAsSingleton()
    {
        // When
        _services.AddNavigationService();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IApplicationContext));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void AddNavigationService_RegistersAppStartupWindowHookAsSingleton()
    {
        // When
        _services.AddNavigationService();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(AppStartupWindowHook));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void AddNavigationService_RegistersPopupNavigationAsSingleton()
    {
        // When
        _services.AddNavigationService();

        // Then
        var descriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(IPopupNavigation));
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
    }
}