using Nkraft.MvvmEssentials.Services;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

internal static class PageRegistryCollectionExtension
{
	public static void AddPageRegistry(this IServiceCollection services, Action<IPageRegistry> configure)
	{
		var registry = new PageRegistry(services);
		configure(registry);
		services.AddSingleton<IPageRegistry>(registry);
	}
}
