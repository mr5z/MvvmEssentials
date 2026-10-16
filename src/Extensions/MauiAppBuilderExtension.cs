using Nkraft.Mopups.Hosting;
using Nkraft.MvvmEssentials.Services;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

public static class MauiAppBuilderExtension
{
	public static MauiAppBuilder ConfigureMvvmEssentials(this MauiAppBuilder builder, Action<IPageRegistry> configurePageRegistry)
		=> builder.ConfigureMvvmEssentials(configurePageRegistry, static _ => { });

	/// <summary>
	/// Configures MvvmEssentials with library-wide <see cref="MvvmEssentialsOptions"/>,
	/// such as how exceptions thrown from ViewModel lifecycle methods are handled.
	/// </summary>
	public static MauiAppBuilder ConfigureMvvmEssentials(
		this MauiAppBuilder builder,
		Action<IPageRegistry> configurePageRegistry,
		Action<MvvmEssentialsOptions> configureOptions)
	{
		builder.ConfigureMopups(); // registers MopupService.Instance as singleton
		builder.Services.AddLifecycleExceptionHandler(configureOptions);
		builder.Services.AddNavigationService();
		builder.Services.AddPageRegistry(configurePageRegistry);
		builder.Services.AddContentViewFactory();
		return builder;
	}
}
