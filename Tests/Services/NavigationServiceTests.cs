using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.UnitTest.Fakes;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class NavigationServiceTests
{
    private IPageFactory _pageFactory = null!;
    private IApplicationContext _applicationContext = null!;
    private INavigationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _pageFactory = Substitute.For<IPageFactory>();
        _applicationContext = Substitute.For<IApplicationContext>();

        // NullLogger avoids Castle DynamicProxy issues with ILogger<NavigationService>
        // where NavigationService is internal
        _sut = new NavigationService(
            NullLogger<NavigationService>.Instance,
            _pageFactory,
            _applicationContext);
    }

    // -----------------------------------------------------------------------
    // NavigateAsync — factory error paths
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WhenGetPageTypesThrows_ReturnsFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Throws(new InvalidOperationException("Page not found."));

        // When
        var result = await _sut.NavigateAsync("//UnknownPage");

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WhenPageListIsEmpty_ReturnsFailure()
    {
        // Given
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([]);

        // When
        var result = await _sut.NavigateAsync("//SomePage");

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // NavigateAsync — absolute single-page navigation
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithAbsolutePathAndSinglePage_SetsMainPage()
    {
        // Given
        var page = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(Page))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(page);

        // When
        var result = await _sut.NavigateAsync("//FakePage");

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_applicationContext.MainPage, Is.SameAs(page));
    }

    // -----------------------------------------------------------------------
    // NavigateAsync — relative path requires existing MainPage
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithRelativePath_WhenNoWindowExists_ReturnsFailure()
    {
        // Given
        var page = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(Page))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(page);

        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateAsync_WithRelativePath_WhenMainPageIsPlainPage_ReturnsFailure()
    {
        // Given
        var page = new Page();
        _pageFactory
            .GetPageTypesFromPath<Page>(Arg.Any<string>())
            .Returns([new PageInfo(typeof(Page))]);
        _pageFactory
            .CreatePage(Arg.Any<PageInfo>(), Arg.Any<INavigationParameters>())
            .Returns(page);

        // Real Window with a plain Page — Window.Page is not virtual so we can't substitute it
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateAsync("FakePage");

        // Then — relative nav on a plain Page is not supported
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // NavigateBackAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateBackAsync_WhenNoWindowExists_ReturnsFailure()
    {
        // Given
        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateBackAsync_WhenMainPageIsPlainPage_ReturnsFailure()
    {
        // Given — real Window with plain Page; SendBackButtonPressed returns false in tests
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    // -----------------------------------------------------------------------
    // NavigateToRootAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateToRootAsync_WhenNoWindowExists_ReturnsFailure()
    {
        // Given
        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public async Task NavigateToRootAsync_WhenMainPageIsNotNavigationPage_ReturnsFailure()
    {
        // Given — real Window with plain Page, not a NavigationPage
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
    }
}

// =============================================================================
// Parameter mapping pipeline — real PageFactory + real PageRegistry + real DI
// =============================================================================

[TestFixture]
public class NavigationServiceParameterMappingTests
{
    private ServiceProvider _serviceProvider = null!;
    private IApplicationContext _applicationContext = null!;
    private INavigationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        // Point AssemblyPageSource at the test assembly so PageFactory
        // can resolve "MappablePage" by name
        services.Configure<NavigationOptions>(options =>
            options.AssemblyPageSource = typeof(MappablePage).Assembly);

        // Real registry — maps MappablePage → MappableViewModel
        IPageRegistry registry = new PageRegistry(services);
        registry.MapPage<MappablePage, MappableViewModel>();
        services.AddSingleton(registry);

        // Real factory + service with NullLoggers (avoids ILogger<internal T> proxy issue)
        services.AddSingleton<ILogger<PageFactory>>(NullLogger<PageFactory>.Instance);
        services.AddSingleton<ILogger<NavigationService>>(NullLogger<NavigationService>.Instance);
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<INavigationService, NavigationService>();

        _applicationContext = Substitute.For<IApplicationContext>();
        services.AddSingleton(_applicationContext);

        _serviceProvider = services.BuildServiceProvider();
        _sut = _serviceProvider.GetRequiredService<INavigationService>();
    }

    [TearDown]
    public void TearDown() => _serviceProvider.Dispose();

    // Helper — resolve the ViewModel that was bound to MainPage after navigation
    private MappableViewModel GetBoundViewModel()
    {
        var page = _applicationContext.MainPage;
        Assert.That(page, Is.Not.Null, "MainPage was not set after navigation");
        var vm = page!.BindingContext as MappableViewModel;
        Assert.That(vm, Is.Not.Null, "BindingContext is not a MappableViewModel");
        return vm!;
    }

    // -----------------------------------------------------------------------
    // INavigationParameters → property mapping
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithINavigationParameters_MapsStringProperty()
    {
        // Given
        var parameters = new NavigationParameters { { "Name", "Alice" } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        Assert.That(GetBoundViewModel().Name, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task NavigateAsync_WithINavigationParameters_MapsIntProperty()
    {
        // Given
        var parameters = new NavigationParameters { { "Age", 30 } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        Assert.That(GetBoundViewModel().Age, Is.EqualTo(30));
    }

    [Test]
    public async Task NavigateAsync_WithINavigationParameters_MapsMultipleProperties()
    {
        // Given
        var parameters = new NavigationParameters
        {
            { "Name", "Bob" },
            { "Age", 25 }
        };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.Name, Is.EqualTo("Bob"));
        Assert.That(vm.Age, Is.EqualTo(25));
    }

    [Test]
    public async Task NavigateAsync_WithINavigationParameters_MapsNullableIntProperty()
    {
        // Given
        var parameters = new NavigationParameters { { "NullableAge", 42 } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        Assert.That(GetBoundViewModel().NullableAge, Is.EqualTo(42));
    }

    // -----------------------------------------------------------------------
    // Query string → property mapping
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithQueryStringParameters_MapsStringProperty()
    {
        // When — query string carries Name
        await _sut.NavigateAsync("//MappablePage?Name=Charlie");

        // Then
        Assert.That(GetBoundViewModel().Name, Is.EqualTo("Charlie"));
    }

    [Test]
    public async Task NavigateAsync_WithQueryStringParameters_MapsIntProperty()
    {
        // When
        await _sut.NavigateAsync("//MappablePage?Age=99");

        // Then
        Assert.That(GetBoundViewModel().Age, Is.EqualTo(99));
    }

    [Test]
    public async Task NavigateAsync_WithQueryStringParameters_MapsMultipleProperties()
    {
        // When
        await _sut.NavigateAsync("//MappablePage?Name=Dave&Age=40");

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.Name, Is.EqualTo("Dave"));
        Assert.That(vm.Age, Is.EqualTo(40));
    }

    // -----------------------------------------------------------------------
    // OnParametersSet is always called
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateAsync_WithParameters_CallsOnParametersSet()
    {
        // Given
        var parameters = new NavigationParameters { { "Name", "Eve" } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        Assert.That(GetBoundViewModel().OnParametersSetCount, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateAsync_WithNoParameters_StillCallsOnParametersSet()
    {
        // When — no parameters passed at all
        await _sut.NavigateAsync("//MappablePage");

        // Then — OnParametersSet is called with an empty NavigationParameters
        Assert.That(GetBoundViewModel().OnParametersSetCount, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateAsync_WithParameters_OnParametersSetReceivesCorrectParameters()
    {
        // Given
        var parameters = new NavigationParameters();
        parameters.Add("Name", "Eve");

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        var lastParams = GetBoundViewModel().LastParameters;
        Assert.That(lastParams, Is.Not.Null);
        Assert.That(lastParams!.ContainsKey("Name"), Is.True);
    }

    // -----------------------------------------------------------------------
    // Unknown / mismatched keys are silently ignored
    // -----------------------------------------------------------------------

    [Test]
    public void NavigateAsync_WithUnknownParameterKey_DoesNotThrow()
    {
        // Given
        var parameters = new NavigationParameters { { "NonExistentProperty", "value" } };

        // Then
        Assert.DoesNotThrowAsync(async() => await _sut.NavigateAsync("//MappablePage", parameters));
    }

    [Test]
    public async Task NavigateAsync_WithTypeMismatchParameter_DoesNotSetProperty()
    {
        // Given — Age is int but we pass a string
        var parameters = new NavigationParameters { { "Age", "not-an-int" } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then — Age stays at default value
        Assert.That(GetBoundViewModel().Age, Is.EqualTo(0));
    }
}
