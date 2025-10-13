# Installation
[![NuGet Version](https://img.shields.io/nuget/v/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![NuGet Pre-release](https://img.shields.io/nuget/vpre/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![GitHub Release](https://img.shields.io/github/release/mr5z/MvvmEssentials.svg?style=flat)](https://github.com/mr5z/MvvmEssentials/packages/385702)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Nkraft.MvvmEssentials.svg)](https://www.nuget.org/packages/Nkraft.MvvmEssentials/)
[![.NET](https://github.com/mr5z/MvvmEssentials/actions/workflows/dotnet.yml/badge.svg)](https://github.com/mr5z/MvvmEssentials/actions/workflows/dotnet.yml)

# Setup
Quick test using this [test project](https://github.com/mr5z/MauiTest1), or just follow the instructions below:

1. Register types in DI container
```cs
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // ..
        builder.Services.AddPageRegistry(registry =>
        {
            // ViewModel and Page naming convention must strictly be followed,
            // page_name + Page
			// vm_name + ViewModel
			// wherein page_name == vm_name
            registry.MapPage<LandingPage, LandingViewModel>()
                .MapPage<MainPage, MainViewModel>()
                .MapPage<LoginPage, LoginViewModel>()
                .MapPage<SettingsPage, SettingsViewModel>()
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
	private readonly ILogger<App> _logger;
	private readonly INavigationService _navigationService;
	private readonly IWindowEventHandler _windowEvent;

	public App(ILogger<App> logger, INavigationService navigationService, IWindowEventHandler windowEvent)
	{
		_logger = logger;
		_navigationService = navigationService;
		_windowEvent = windowEvent;

		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// I know Task is not awaited.
		// I actually don't know where is the better place to do this.
		// Help needed 🙏.

		var result = _navigationService.Absolute(withNavigation: false)
			.Push<LandingViewModel>() // replace with whatever initial ViewModel your app should use
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

## NavigationService
```cs
interface INavigationService
{
	// Under the hood, it detects which current page type is currently active,
	// and performs either a page replacement, or push page on the stack if its NavigationPage.
	// I recommend to use the extensions instead of this.
	Task<IResult> NavigateAsync(string path, INavigationParameters? parameters = null, bool animated = true);

	// Wraps Navigation.PopAsync()
	Task<IResult> NavigateBackAsync(bool animated = true);

	// Wraps Navigation.PopToRootAsync()
	Task<IResult> NavigateToRootAsync(INavigationParameters? parameters = null, bool animated = true);
}
```

### NavigationExtension Examples
1. Page replacement
```cs
await _navigationService.Absolute(withNavigation: true)
	.Push<FirstViewModel, object>(new { A = 1 }) // .Push can only handle "primitive" data types
	.Push<SecondViewModel, object>(new { B = 2 })
	.Push<ThirdViewModel, object>(new { C = 3 })
	.NavigateAsync();
// Constructs "//NavigationPage/FirstPage?A=1/SecondPage?B=2/ThirdPage?C=3"
```
2. Navigation with different types of parameters
```cs
// Pass parameters via object type
await _navigationService.NavigateAsync<LoginViewModel, object>(new { ErrorMessage = "Session expired", Test = 1 });

// Pass parameters via custom type
record LoginParameters(string ErrorMessage, int Test);
await _navigationService.NavigateAsync<LoginViewModel, LoginParameters>(new("Session expired", 1));

// Pass parameters via INavigationParameters, which is the "superior" of the 2 previous variants
INavigationParameters parameters = new NavigationParameters();
parameters.Add("ErrorMessage", "Session expired");
parameters.Add("Test", 1);
await _navigationService.NavigateAsync<LoginViewModel>(parameters);

// LoginViewModel.cs
class LoginViewModel : PageViewModel
{
	// Will automatically map to this property
	public string? ErrorMessage { get; set; }

	// Alternatively for no mapped properties
	public override void OnParametersSet(INavigationParameters parameters)
	{
		if (parameters.TryGetValue<int>("Test", out var testValue))
		{
			// ..
		}
	}
}

```
3. Contextual navigation
```cs
// Will either replace the page if the active page is not a NavigationPage
// or will push on NavigationPage's stack if it is.
await _navigationService.NavigateAsync<AccountViewModel>();
```
4. Select tab of TabbedPage
```cs
await _navigationService.Absolute(withNavigation: false)
	.Push<MainViewModel, object>(new { SelectedTabIndex = 2 }) // switches to 3rd tab
	.NavigateAsync();
```

## NavigationPage
```cs
// Inject INavigationService into your ViewModel
internal class LandingViewModel(INavigationService navigationService) : PageViewModel
{
    private readonly INavigationService _navigationService = navigationService;

    public override async Task OnInitializedAsync()
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

1. Register tabs in DI container
```cs
// These are tabs and should not be mapped to their associated pages
// because we are binding them via XAML
builder.Services.AddTransient<HomeViewModel>();
builder.Services.AddTransient<SettingsViewModel>();

builder.Services.AddPageRegistry(registry =>
{
	// This is the tab host
	registry.MapPage<MainPage, MainViewModel>()
		;
});
```

2. Define the structure of your tabs in XAML
```xaml
<?xml version="1.0" encoding="utf-8" ?>
<TabbedPage
	xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
	xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
	xmlns:behaviors="clr-namespace:Nkraft.MvvmEssentials.Behaviors;assembly=Nkraft.MvvmEssentials"
	xmlns:local="clr-namespace:MauiApp1"
	xmlns:android="clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;assembly=Microsoft.Maui.Controls"
	android:TabbedPage.ToolbarPlacement="Bottom"
	x:DataType="local:MainViewModel"
	x:Class="MauiApp1.MainPage">

	<!-- Include this for tab selection via VM to work -->
	<TabbedPage.Behaviors>
		<behaviors:TabSelectionBehavior />
	</TabbedPage.Behaviors>

	<NavigationPage Title="Home">
		<x:Arguments>
			<local:HomePage BindingContext="{Binding HomeViewModel}" />
		</x:Arguments>
	</NavigationPage>

	<NavigationPage Title="Settings">
		<x:Arguments>
			<local:SettingsPage BindingContext="{Binding SettingsViewModel}" />
		</x:Arguments>
	</NavigationPage>

</TabbedPage>
```

3. Inject ViewModels in the host VM and inherit from `TabHostViewModel`
```cs
public class MainViewModel(HomeViewModel homeViewModel, SettingsViewModel settingsViewModel) : TabHostViewModel
{
	public override ImmutableArray<TabViewModel> GetTabs() => [HomeViewModel, SettingsViewModel];

	public HomeViewModel HomeViewModel { get; } = homeViewModel;

	public SettingsViewModel SettingsViewModel { get; } = settingsViewModel;
}
```

4. Define tab ViewModels
```cs
public partial class HomeViewModel(ISemanticScreenReader screenReader) : TabViewModel
{
	private readonly ISemanticScreenReader _screenReader = screenReader;

	public override void OnTabSelected()
	{
		base.OnTabSelected();

		Console.WriteLine("Home tab selected");
	}

	public override void OnTabUnselected()
	{
		base.OnTabUnselected();

		Console.WriteLine("Bye!");
	}

	// Really cool attribute from CommunityToolkit.Mvvm
	[RelayCommand]
	private void IncreaseCount()
	{
		Count++;

		if (Count == 1)
			CountButtonText = $"Clicked {Count} time";
		else
			CountButtonText = $"Clicked {Count} times";

		_screenReader.Announce(CountButtonText);
	}

	// We assume you're using Fody PropertyChanged

	public int Count { get; set; }

	public string CountButtonText { get; set; } = "Click me";
}

```

# FlyoutPage
```cs
// TODO
```

# Contribution
Any help welcome. Thanks!
