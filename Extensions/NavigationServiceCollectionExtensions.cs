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
		services.AddSingleton<INavigationService, NavigationService>();
		return services;
	}
}