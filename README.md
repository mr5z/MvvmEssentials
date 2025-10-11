# Setup

1. Follow the code setup below:
```cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // ..
        builder.Services.AddPageRegistry(registry =>
        {
            // ViewModel and Page naming convention must strictly be followed
            // i.e., <page_name>Page, <viewmodel_name>ViewModel wherein page_name == viewmodel_name
            registry.MapPage<LandingPage, LandingViewModel>()
                .MapPage<MainPage, MainViewModel>()
                .MapPage<LoginPage, LoginViewModel>()
                .MapPage<LoginPage, LoginViewModel>()
                // ..
                ;
        });
    
        builder.Services.AddNavigationService(options =>
            options.AssemblyPageSource = Assembly.GetExecutingAssembly()
        );

        // ..
    }
}
```
2. Delete any `Shell` related because it is garbage.
3. Under `App.xaml.cs`:
```cs
public partial class App
{
    private readonly INavigationService _navigationService;
    private readonly IWindowEventHandler _windowEvent;

    public App(ILogger<App> logger, INavigationService navigationService, IWindowEventHandler windowEvent)
    {
        _navigationService = navigationService;
        _windowEvent = windowEvent;
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // I know Task is not awaited.
        // I actually don't know where is the better place to do this.
        // Help needed, please 🙏.

        var result = _navigationService.Absolute(withNavigation: false)
            .Push<LandingViewModel>() // replace with whatever first ViewModel your app should use
            .NavigateAsync()
            .GetAwaiter()
            .GetResult();
    
        if (result.IsFailure)
        {
            _logger.LogError(result.ErrorMessage);
            Application.Current?.Quit();
            throw new Exception($"Application quitting due to: {result.ErrorMessage}");
        }
    
        var window = base.CreateWindow(activationState);
    
        window.Created += (s, e) => _windowEvent.OnCreated();
        window.Destroying += (s, e) => _windowEvent.OnDestroying();
        window.Activated += (s, e) => _windowEvent.OnActivated();
        window.Deactivated += (s, e) => _windowEvent.OnDeactivated();
        window.Resumed += (s, e) => _windowEvent.OnResumed();
        window.Stopped += (s, e) => _windowEvent.OnStopped();
    
        return window;
    }

}
```

# Usage
## NavigationPage
```cs
// Inject INavigationService into your ViewModel
internal class LandingViewModel(INavigationService navigationService) : PageViewModel
{
    private readonly INavigationService _navigationService = navigationService;

    public override Task OnInitializedAsync()
    {
        awwait GoToLoginAsync();
    }

    private async Task GoToLoginAsync()
    {
        await _navigationService.NavigateToAsync<LoginViewModel>();
    }
}
```

## TabbedPage
```cs
// TODO
```

## TabbedPage + NavigationPage
```cs
// TODO
```

# Contribution
Any help welcome. Thanks!
