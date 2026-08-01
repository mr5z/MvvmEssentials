using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.CrossUtility.Patterns;
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
        
        DispatcherProvider.SetCurrent(new TestDispatcherProvider());
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
    public async Task NavigateBackAsync_WhenNoWindowExists_ReturnsInvalidState()
    {
        // Given
        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    [Test]
    public async Task NavigateBackAsync_WhenMainPageIsPlainPage_ReturnsNotHandled()
    {
        // Given — real Window with plain Page; SendBackButtonPressed returns false in tests
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
    }

    // -----------------------------------------------------------------------
    // NavigateToRootAsync
    // -----------------------------------------------------------------------
    [Test]
    public async Task NavigateToRootAsync_WhenNoWindowExists_ReturnsInvalidState()
    {
        // Given
        _applicationContext.Windows.Returns([]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.InvalidState));
    }

    [Test]
    public async Task NavigateToRootAsync_WhenMainPageIsNotNavigationPage_ReturnsNotSupported()
    {
        // Given — real Window with plain Page, not a NavigationPage
        var window = new Window(new Page());
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotSupported));
    }

    [Test]
    public async Task NavigateToRootAsync_WhenAlreadyAtRoot_ReturnsNotHandled()
    {
        // Given — NavigationPage with only 1 page (nothing to pop to root from)
        var navigationPage = new NavigationPage(new Page());
        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
    }

    [Test]
    public async Task NavigateToRootAsync_WhenRootDoesNotImplementIRootPageNavigated_ReturnsSuccess()
    {
        // Given — plain root page, no lifecycle hook to invoke
        var navigationPage = new NavigationPage(new Page());
        await navigationPage.PushAsync(new Page());

        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.Navigation.NavigationStack.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateToRootAsync_WhenRootImplementsIRootPageNavigated_InvokesLifecycleHooksAndReturnsSuccess()
    {
        // Given — root page's BindingContext implements IRootPageNavigated
        var rootViewModel = new TrackableRootPageNavigatedViewModel();
        var rootPage = new Page { BindingContext = rootViewModel };

        var navigationPage = new NavigationPage(rootPage);
        await navigationPage.PushAsync(new Page());

        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        var parameters = new NavigationParameters { { "Key", "Value" } };

        // When
        var result = await _sut.NavigateToRootAsync(parameters);

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(rootViewModel.OnNavigatedToRootCount, Is.EqualTo(1));
        Assert.That(rootViewModel.OnNavigatedToRootAsyncCount, Is.EqualTo(1));
        Assert.That(rootViewModel.LastParameters, Is.SameAs(parameters));
    }

    [Test]
    public async Task NavigateToRootAsync_WithNullParameters_PassesEmptyNavigationParameters()
    {
        // Given
        var rootViewModel = new TrackableRootPageNavigatedViewModel();
        var rootPage = new Page { BindingContext = rootViewModel };

        var navigationPage = new NavigationPage(rootPage);
        await navigationPage.PushAsync(new Page());

        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        // When
        await _sut.NavigateToRootAsync(parameters: null);

        // Then
        Assert.That(rootViewModel.LastParameters, Is.Not.Null);
        Assert.That(rootViewModel.LastParameters!.IsEmpty, Is.True);
    }

    [Test]
    public async Task NavigateToRootAsync_WhenLifecycleHookThrows_ReturnsGeneral()
    {
        // Given — root ViewModel throws during OnNavigatedToRootAsync
        var rootViewModel = new TrackableRootPageNavigatedViewModel { ThrowOnAsync = true };
        var rootPage = new Page { BindingContext = rootViewModel };

        var navigationPage = new NavigationPage(rootPage);
        await navigationPage.PushAsync(new Page());

        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateToRootAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.General));
    }
    
    
    // -----------------------------------------------------------------------
    // NavigateBackAsync — page-hierarchy resolution matrix
    // -----------------------------------------------------------------------

    [Test]
    public async Task NavigateBackAsync_WhenCurrentPageIsNavigationPageWithMultiplePages_PopsStack()
    {
        // Given — plain NavigationPage, 2 pages on the stack
        var navigationPage = new NavigationPage(new Page());
        await navigationPage.PushAsync(new Page());

        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.Navigation.NavigationStack.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateBackAsync_WhenNavigationPageIsAtRoot_ReturnsNotHandled()
    {
        // Given — NavigationPage with only 1 page (nothing to pop)
        var navigationPage = new NavigationPage(new Page());
        var window = new Window(navigationPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
    }

    [Test]
    public async Task NavigateBackAsync_WhenCurrentTabIsNavigationPageWithMultiplePages_PopsStack()
    {
        // Given — TabbedPage whose current tab is a NavigationPage with 2 pages
        var navigationPage = new NavigationPage(new Page());
        await navigationPage.PushAsync(new Page());

        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(navigationPage);

        var window = new Window(tabbedPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.Navigation.NavigationStack.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateBackAsync_WhenTabbedPageCurrentTabIsPlainPage_FallsBackToSendBackButtonPressed()
    {
        // Given — TabbedPage whose current tab is NOT a NavigationPage
        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(new Page());

        var window = new Window(tabbedPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then — no NavigationPage resolvable anywhere in hierarchy; SendBackButtonPressed() returns false in tests
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
    }

    [Test]
    public async Task NavigateBackAsync_WhenFlyoutDetailIsNavigationPageWithMultiplePages_PopsStack()
    {
        // Given — FlyoutPage.Detail is a NavigationPage with 2 pages
        var navigationPage = new NavigationPage(new Page());
        await navigationPage.PushAsync(new Page());

        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "Menu" },
            Detail = navigationPage
        };

        var window = new Window(flyoutPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.Navigation.NavigationStack.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateBackAsync_WhenFlyoutDetailIsTabbedPageWithNavigationPage_PopsNestedStack()
    {
        // Given — FlyoutPage.Detail is a TabbedPage whose current tab is a NavigationPage
        // This is the case the recursive NavigationHelper.FindNavigationPage resolves
        // but the old inline switch (flyoutPage.Detail as NavigationPage) could not.
        var navigationPage = new NavigationPage(new Page());
        await navigationPage.PushAsync(new Page());

        var tabbedPage = new TabbedPage();
        tabbedPage.Children.Add(navigationPage);

        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "Menu" },
            Detail = tabbedPage
        };

        var window = new Window(flyoutPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(navigationPage.Navigation.NavigationStack.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task NavigateBackAsync_WhenFlyoutDetailIsPlainPage_FallsBackToSendBackButtonPressed()
    {
        // Given — FlyoutPage.Detail is neither a NavigationPage nor a TabbedPage wrapping one
        var flyoutPage = new FlyoutPage
        {
            Flyout = new ContentPage { Title = "Menu" },
            Detail = new ContentPage()
        };

        var window = new Window(flyoutPage);
        _applicationContext.Windows.Returns([window]);

        // When
        var result = await _sut.NavigateBackAsync();

        // Then
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(ErrorCode.NotHandled));
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

        // Real registry — maps MappablePage → MappableViewModel
        IPageRegistry registry = new PageRegistry(services);
        registry.MapPage<MappablePage, MappableViewModel>();
        registry.MapPage<MappableSecondPage, MappableViewModel>();
        services.AddSingleton(registry);

        // Real factory + service with NullLoggers (avoids ILogger<internal T> proxy issue)
        services.AddSingleton<ILogger<PageFactory>>(NullLogger<PageFactory>.Instance);
        services.AddSingleton<ILogger<NavigationService>>(NullLogger<NavigationService>.Instance);
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton(Substitute.For<IDispatcher>());

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

        // MainPage may be the target page itself, or a NavigationPage wrapping it
        var target = page is NavigationPage navPage ? navPage.CurrentPage : page;
        Assert.That(target, Is.Not.Null, "NavigationPage has no CurrentPage");

        var vm = target!.BindingContext as MappableViewModel;
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
    
    [Test]
    public async Task NavigateAsync_WithQueryString_DeliversParametersToOnParametersSet()
    {
        // When
        await _sut.NavigateAsync("//MappablePage?Age=99");

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.LastParameters, Is.Not.Null);
        Assert.That(vm.LastParameters!.ContainsKey("Age"), Is.True);
    }

    [Test]
    public async Task NavigateAsync_WithQueryStringTargetingUnattributedProperty_StillDeliversParameter()
    {
        // When — the gate blocks the write, not the delivery
        await _sut.NavigateAsync("//MappablePage?UnmappedAge=77");

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.UnmappedAge, Is.EqualTo(0));
        Assert.That(vm.LastParameters!.ContainsKey("UnmappedAge"), Is.True);
    }

    [Test]
    public async Task NavigateAsync_WithQueryStringAndDirectParameters_MergesBoth()
    {
        // Given
        var parameters = new NavigationParameters { { "Name", "Alice" } };

        // When
        await _sut.NavigateAsync("//MappablePage?Age=99", parameters);

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.Name, Is.EqualTo("Alice"));
        Assert.That(vm.Age, Is.EqualTo(99));
        Assert.That(vm.LastParameters!.ContainsKey("Name"), Is.True);
        Assert.That(vm.LastParameters!.ContainsKey("Age"), Is.True);
    }

    [Test]
    public async Task NavigateAsync_WithSameKeyInQueryStringAndParameters_DirectParameterWins()
    {
        // Given — duplicate key across both sources previously risked a double write
        var parameters = new NavigationParameters { { "Age", 30 } };

        // When
        await _sut.NavigateAsync("//MappablePage?Age=99", parameters);

        // Then
        Assert.That(GetBoundViewModel().Age, Is.EqualTo(30));
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
    
    [Test]
    public async Task NavigateAsync_WithUnattributedPropertyKey_DoesNotSetProperty()
    {
        // Given — key matches a real property, but it has no [NavigationParameter]
        var parameters = new NavigationParameters { { "UnmappedName", "Mallory" } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        Assert.That(GetBoundViewModel().UnmappedName, Is.Null);
    }

    [Test]
    public async Task NavigateAsync_WithUnattributedPrivateSetProperty_DoesNotSetProperty()
    {
        // Given — private setters were reachable before the gate (BindingFlags.NonPublic)
        var parameters = new NavigationParameters { { "UnmappedPrivateSet", "Mallory" } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        Assert.That(GetBoundViewModel().UnmappedPrivateSet, Is.Null);
    }

    [Test]
    public async Task NavigateAsync_WithQueryStringTargetingUnattributedProperty_DoesNotSetProperty()
    {
        // When — the gate must apply to the query-string path too, not just INavigationParameters
        await _sut.NavigateAsync("//MappablePage?UnmappedAge=77");

        // Then
        Assert.That(GetBoundViewModel().UnmappedAge, Is.EqualTo(0));
    }

    [Test]
    public async Task NavigateAsync_WithMixedKeys_MapsOnlyAttributedProperties()
    {
        // Given
        var parameters = new NavigationParameters
        {
            { "Name", "Alice" },
            { "UnmappedName", "Mallory" }
        };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.Name, Is.EqualTo("Alice"));
        Assert.That(vm.UnmappedName, Is.Null);
    }

    [Test]
    public async Task NavigateAsync_WithUnattributedKey_StillDeliversParametersToOnParametersSet()
    {
        // Given — gating property writes must not filter the dictionary itself
        var parameters = new NavigationParameters { { "UnmappedName", "Mallory" } };

        // When
        await _sut.NavigateAsync("//MappablePage", parameters);

        // Then
        var vm = GetBoundViewModel();
        Assert.That(vm.OnParametersSetCount, Is.EqualTo(1));
        Assert.That(vm.LastParameters!.ContainsKey("UnmappedName"), Is.True);
    }

    [Test]
    public void NavigateAsync_WithKeyMatchingReadOnlyProperty_DoesNotThrow()
    {
        // Given — no setter and no attribute
        var parameters = new NavigationParameters { { "ReadOnlyProp", "value" } };

        // Then
        Assert.DoesNotThrowAsync(async () => await _sut.NavigateAsync("//MappablePage", parameters));
    }
    
    [Test]
    public async Task NavigateAsync_ViaPushDestination_BindsPropertiesOnArrival()
    {
        // Given — end-to-end: With() -> Push() -> query string -> property
        var destination = new PageDestination(
            "MappablePage",
            new NavigationParameters { { "Name", "Alice" }, { "Age", 30 } });

        // When
        var result = await _sut.Absolute().Push(destination).NavigateAsync();

        // Then
        var vm = GetBoundViewModel();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(vm.Name, Is.EqualTo("Alice"));
        Assert.That(vm.Age, Is.EqualTo(30));
    }

    [Test]
    public async Task NavigateAsync_ViaPushDestination_RoundTripsBool()
    {
        // Given — IsPathSafe permits bool; this fails if QueryStringHelper.ToDictionary
        // hands back a string, because AreTypesEqual demands an exact match
        var destination = new PageDestination(
            "MappablePage",
            new NavigationParameters { { "IsEditing", true } });

        // When
        await _sut.Absolute().Push(destination).NavigateAsync();

        // Then
        Assert.That(GetBoundViewModel().IsEditing, Is.True);
    }

    [Test]
    public async Task NavigateAsync_ViaPushDestination_RoundTripsGuid()
    {
        // Given — same question for Guid
        var id = Guid.NewGuid();
        var destination = new PageDestination(
            "MappablePage",
            new NavigationParameters { { "CorrelationId", id } });

        // When
        await _sut.Absolute().Push(destination).NavigateAsync();

        // Then
        Assert.That(GetBoundViewModel().CorrelationId, Is.EqualTo(id));
    }
    
    [Test]
    public async Task HandlePageUnloaded_WhenPageIsUnloaded_DisposesItsViewModel()
    {
        // Given
        await _sut.NavigateAsync("//MappablePage");
        var viewModel = GetBoundViewModel();
        var page = _applicationContext.MainPage!;

        // When
        ((PageFactory)_serviceProvider.GetRequiredService<IPageFactory>()).HandlePageUnloaded(page);

        // Then
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1));
    }
    
    [Test]
    public async Task NavigateAsync_CalledTwice_BindsADistinctViewModelInstanceEachTime()
    {
        // Given
        await _sut.NavigateAsync("//MappablePage");
        var first = GetBoundViewModel();

        // When
        await _sut.NavigateAsync("//MappablePage");
        var second = GetBoundViewModel();

        // Then — each page gets its own DI scope, so the scoped ViewModel is not shared
        Assert.That(second, Is.Not.SameAs(first));
    }
}
