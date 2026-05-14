using Nkraft.MvvmEssentials.Services.Navigation;
using System.Reflection;

namespace Nkraft.MvvmEssentials.Extensions;

public static class MauiAppBuilderExtension
{
	public static MauiAppBuilder ConfigureMvvmEssentials(this MauiAppBuilder builder, Assembly executingAssembly, Action<IPageRegistry> configurePageRegistry)
	{
		builder.Services.AddNavigationService(options => options.AssemblyPageSource = executingAssembly);
		builder.Services.AddPageRegistry(configurePageRegistry);
		return builder;
	}
}
