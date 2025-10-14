using Mopups.Services;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Extensions;

public static class NavigationServiceCollectionExtension
{
	public static IServiceCollection AddNavigationService(
		this IServiceCollection services,
		Action<NavigationOptions> configure)
	{
		services.Configure(configure);
		services.AddSingleton<IWindowEventHandler, WindowEventHandler>();
		services.AddSingleton<IPageFactory, PageFactory>();
		services.AddSingleton<INavigationService, NavigationService>();

		// TODO move to different extension
		services.AddSingleton<IPopupService, PopupService>();
		services.AddSingleton(MopupService.Instance);
		return services;
	}
}