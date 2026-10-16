using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Pages;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

internal static class NavigationServiceCollectionExtension
{
	public static void AddNavigationService(this IServiceCollection services)
	{
		services.AddSingleton<IPageFactory, PageFactory>();
		services.AddSingleton<INavigationService, NavigationService>();

		// TODO move to different extension
		services.AddSingleton<IPopupService, PopupService>();
		services.AddSingleton<IModalService, ModalService>();
		services.AddSingleton<IApplicationContext, ApplicationContext>();
		
		services.AddSingleton<AppStartupWindowHook>();
	}

	public static void AddLifecycleExceptionHandler(this IServiceCollection services, Action<MvvmEssentialsOptions> configureOptions)
	{
		services.Configure(configureOptions);
		services.AddSingleton<ILifecycleExceptionHandler, LifecycleExceptionHandler>();
	}
}
