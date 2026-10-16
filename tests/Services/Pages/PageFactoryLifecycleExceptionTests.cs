using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Pages;

/// <summary>
/// Regression tests: an exception from a ViewModel lifecycle method must not escape into MAUI
/// (which would leave its navigation state half-updated); it goes to the lifecycle exception handler.
/// </summary>
[TestFixture]
public class PageFactoryLifecycleExceptionTests
{
    private ServiceProvider _serviceProvider = null!;
    private ILifecycleExceptionHandler _handler = null!;
    private ThrowingLifecycleSettings _settings = null!;
    private PageFactory _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        IPageRegistry registry = new PageRegistry(services);
        registry.MapPage<ThrowingLifecyclePage, ThrowingLifecycleViewModel>();
        services.AddSingleton(registry);

        _settings = new ThrowingLifecycleSettings();
        services.AddSingleton(_settings);

        _handler = Substitute.For<ILifecycleExceptionHandler>();
        services.AddSingleton(_handler);

        services.AddSingleton<ILogger<PageFactory>>(NullLogger<PageFactory>.Instance);
        services.AddSingleton<IPageFactory, PageFactory>();

        _serviceProvider = services.BuildServiceProvider();
        _sut = (PageFactory)_serviceProvider.GetRequiredService<IPageFactory>();
    }

    [TearDown]
    public void TearDown() => _serviceProvider.Dispose();

    private (Page Page, ThrowingLifecycleViewModel ViewModel) CreatePage()
    {
        var page = ((IPageFactory)_sut).CreatePage(new PageInfo(typeof(ThrowingLifecyclePage)));
        return (page, (ThrowingLifecycleViewModel)page.BindingContext);
    }

    [Test]
    public void Push_WhenOnNavigatedToThrows_NavigationCompletesAndHandlerIsNotified()
    {
        // Given
        _settings.ThrowOnNavigatedTo = true;
        var (page, viewModel) = CreatePage();
        var navigationPage = new NavigationPage(new Page());

        // When — MAUI raises NavigatedTo from inside its own push
        Assert.DoesNotThrowAsync(() => navigationPage.PushAsync(page, animated: false));

        // Then — MAUI's state is consistent, and the failure was reported instead of thrown
        Assert.That(navigationPage.CurrentPage, Is.SameAs(page));
        _handler.Received(1).Handle(
            Arg.Is<Exception>(e => e.Message == ThrowingLifecycleViewModel.NavigatedToMessage),
            nameof(IPageNavigated.OnPageNavigatedTo),
            viewModel);
    }

    [Test]
    public void Push_WhenOnNavigatedToThrows_AsyncHookStillRuns()
    {
        // Given
        _settings.ThrowOnNavigatedTo = true;
        var (page, viewModel) = CreatePage();
        var navigationPage = new NavigationPage(new Page());

        // When
        Assert.DoesNotThrowAsync(() => navigationPage.PushAsync(page, animated: false));

        // Then
        Assert.That(viewModel.NavigatedToAsyncCount, Is.EqualTo(1));
    }

    [Test]
    public void Push_WhenAsyncHookThrowsBeforeReturningTask_IsReportedNotThrown()
    {
        // Given — a non-async override (`Task OnNavigatedToAsync() => throw ...`)
        _settings.ThrowOnNavigatedToAsyncSynchronously = true;
        var (page, viewModel) = CreatePage();
        var navigationPage = new NavigationPage(new Page());

        // When
        Assert.DoesNotThrowAsync(() => navigationPage.PushAsync(page, animated: false));

        // Then
        _handler.Received(1).Handle(
            Arg.Any<Exception>(),
            nameof(IPageNavigated.OnPageNavigatedToAsync),
            viewModel);
    }

    [Test]
    public void HandlePageUnloaded_WhenOnPageUnloadedThrows_StillDisposesScopeAndRaisesEvent()
    {
        // Given
        _settings.ThrowOnUnloaded = true;
        var (page, viewModel) = CreatePage();
        Page? unloaded = null;
        _sut.PageUnloaded += (_, p) => unloaded = p;

        // When
        Assert.DoesNotThrow(() => _sut.HandlePageUnloaded(page));

        // Then
        Assert.That(viewModel.DisposeCount, Is.EqualTo(1), "page scope was not disposed");
        Assert.That(unloaded, Is.SameAs(page));
        _handler.Received(1).Handle(Arg.Any<Exception>(), nameof(IPageLoad.OnPageUnloaded), viewModel);
    }
}

internal sealed class ThrowingLifecycleSettings
{
    public bool ThrowOnNavigatedTo { get; set; }
    public bool ThrowOnNavigatedToAsyncSynchronously { get; set; }
    public bool ThrowOnUnloaded { get; set; }
}

internal class ThrowingLifecyclePage : Page { }

internal class ThrowingLifecycleViewModel(ThrowingLifecycleSettings settings) : PageViewModel
{
    public const string NavigatedToMessage = "OnNavigatedTo boom";

    public int NavigatedToAsyncCount { get; private set; }
    public int DisposeCount { get; private set; }

    protected override void OnNavigatedTo()
    {
        if (settings.ThrowOnNavigatedTo)
            throw new InvalidOperationException(NavigatedToMessage);
    }

    protected override Task OnNavigatedToAsync()
    {
        if (settings.ThrowOnNavigatedToAsyncSynchronously)
            throw new InvalidOperationException("OnNavigatedToAsync boom");

        NavigatedToAsyncCount++;
        return Task.CompletedTask;
    }

    protected override void OnPageUnloaded()
    {
        if (settings.ThrowOnUnloaded)
            throw new InvalidOperationException("OnPageUnloaded boom");
    }

    protected override void OnDispose() => DisposeCount++;
}
