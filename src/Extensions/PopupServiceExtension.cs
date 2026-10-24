using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.ViewModels;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

public static class PopupServiceExtension
{
	extension(IPopupService popupService)
	{
		public async Task<Result<TResult>> PresentAsync<TResult>(
			PopupDestination<TResult> destination, bool animated = true)
			=> await IPopupService.PresentAsync<TResult>(
				popupService, destination.PopupName, destination.Parameters, animated);
		
		public async Task<Result<TResult>> PresentAsync<TViewModel, TResult>(
			INavigationParameters? parameters = null, bool animated = true)
			where TViewModel : IPopupViewModel<TResult>
			=> await IPopupService.PresentAsync<TResult>(
				popupService,
				PageHelper.ToPageName<TViewModel>(PagePattern.Popup),
				parameters ?? new NavigationParameters(),
				animated);

		public async Task<IResult> DismissAsync<TViewModel>(bool animated = true)
			where TViewModel : IPopupViewModel
		{
			var popupName = PageHelper.ToPageName<TViewModel>(PagePattern.Popup);
			return await popupService.DismissAsync(popupName, animated);
		}
		
		private static Task<Result<TResult>> PresentAsync<TResult>(
			IPopupService popup, string popupName, INavigationParameters parameters, bool animated)
			=> Presenting.PresentForResultAsync<TResult>(
				p => popup.PresentAsync(popupName, p, animated), parameters, popupName);
	}
}
