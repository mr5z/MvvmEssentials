using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Extensions;

public static class PageRegistryCollectionExtension
{
	public static IServiceCollection AddPageRegistry(this IServiceCollection services, Action<IPageRegistry> configure)
	{
		var registry = new PageRegistry(services);
		configure(registry);
		services.AddSingleton<IPageRegistry>(registry);
		return services;
	}
}
