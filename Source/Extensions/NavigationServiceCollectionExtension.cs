using Mopups.Interfaces;
using Mopups.Services;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

internal static class NavigationServiceCollectionExtension
{
	public static void AddNavigationService(this IServiceCollection services,
		Action<NavigationOptions> configure)
	{
		services.Configure(configure);
		services.AddSingleton<IPageFactory, PageFactory>();
		services.AddSingleton<INavigationService, NavigationService>();

		// TODO move to different extension
		services.AddSingleton<IPopupService, PopupService>();
		services.AddSingleton<IApplicationContext, ApplicationContext>();
		services.AddSingleton<IPopupNavigation>(_ => MopupService.Instance);
		
		services.AddSingleton<AppStartupWindowHook>();
	}
}