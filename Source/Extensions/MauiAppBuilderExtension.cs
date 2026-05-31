using System.Reflection;
using Nkraft.MvvmEssentials.Services;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

public static class MauiAppBuilderExtension
{
	public static MauiAppBuilder ConfigureMvvmEssentials(this MauiAppBuilder builder, Action<IPageRegistry> configurePageRegistry)
	{
		builder.Services.AddNavigationService();
		builder.Services.AddPageRegistry(configurePageRegistry);
		builder.Services.AddContentViewFactory();
		return builder;
	}
}
