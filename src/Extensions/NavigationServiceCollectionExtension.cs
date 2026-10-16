using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Pages;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

internal static class NavigationServiceCollectionExtension
{
	extension(IServiceCollection services)
	{
		public void AddNavigationService()
		{
			services.AddSingleton<IPageFactory, PageFactory>();
			services.AddSingleton<INavigationService, NavigationService>();

			// TODO move to different extension
			services.AddSingleton<IPopupService, PopupService>();
			services.AddSingleton<IModalService, ModalService>();
			services.AddSingleton<IApplicationContext, ApplicationContext>();
		
			services.AddSingleton<AppStartupWindowHook>();
		}

		public void AddLifecycleExceptionHandler(Action<MvvmEssentialsOptions> configureOptions)
		{
			services.Configure(configureOptions);
			services.AddSingleton<ILifecycleExceptionHandler, LifecycleExceptionHandler>();
		}
	}
}
