using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class NavigationServiceScopeLifetimeTests
{
    private ServiceProvider _serviceProvider = null!;
    private DisposalTracker _tracker = null!;
    private INavigationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        IPageRegistry registry = new PageRegistry(services);
        registry.MapPage<LeakProbePage, LeakProbeViewModel>();
        registry.MapPage<LeakProbeSecondPage, LeakProbeViewModel>();
        services.AddSingleton(registry);

        services.AddSingleton<DisposalTracker>();

        services.AddSingleton<ILogger<PageFactory>>(NullLogger<PageFactory>.Instance);
        services.AddSingleton<ILogger<NavigationService>>(NullLogger<NavigationService>.Instance);
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton(Substitute.For<IDispatcher>());
        services.AddSingleton(Substitute.For<IApplicationContext>());

        _serviceProvider = services.BuildServiceProvider();
        _tracker = _serviceProvider.GetRequiredService<DisposalTracker>();
        _sut = _serviceProvider.GetRequiredService<INavigationService>();
    }
    
    [TearDown]
    public void TearDown() => _serviceProvider.Dispose();

    [Test]
    public async Task NavigateAsync_WhenMultiSegmentAbsolutePathHasNoNavigationPage_DisposesOrphanedScopes()
    {
        // Given

        // When
        var result = await _sut.NavigateAsync("//LeakProbePage/LeakProbeSecondPage");

        // Then — navigation fails, as designed
        Assert.That(result.IsFailure, Is.True);

        // ...but every ViewModel it materialized along the way must still be disposed
        Assert.That(_tracker.Created, Has.Count.EqualTo(2), "both pages were materialized");
        Assert.That(_tracker.Created.Select(vm => vm.DisposeCount), Is.All.EqualTo(1));
    }
}