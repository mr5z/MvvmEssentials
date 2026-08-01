using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
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
				PageHelper.ToPageName<TViewModel>("Popup"),
				parameters ?? new NavigationParameters(),
				animated);

		public async Task<IResult> DismissAsync<TViewModel>(bool animated = true)
			where TViewModel : IPopupViewModel
		{
			var popupName = PageHelper.ToPageName<TViewModel>("Popup");
			return await popupService.DismissAsync(popupName, animated);
		}
		
		private static async Task<Result<TResult>> PresentAsync<TResult>(
			IPopupService popup, string popupName, INavigationParameters parameters, bool animated)
		{
			var tcs = new TaskCompletionSource<TResult>();
			parameters[NavigationHints.PopupCompletionParam] = tcs;

			var navResult = await popup.PresentAsync(popupName, parameters, animated);
			if (navResult.IsFailure)
				return Result.Fail<TResult>(ErrorCode.InvalidState, "Failed to display popup '{PopupName}'.", popupName);

			try
			{
				var popupResult = await tcs.Task;
				return Result.Ok(popupResult);
			}
			catch (TaskCanceledException)
			{
				const string message = "Popup '{PopupName}' has been cancelled.";
				// Intentionally discarding the result since we're fairly certain this is a canceled operation
				// and there's no more information to extract from that state
				_ = await popup.DismissAsync(popupName, animated);
				return Result.Fail<TResult>(ErrorCode.Cancelled, message, popupName);
			}
			catch (Exception ex)
			{
				const string message = "Failed to dismiss popup '{PopupName}'; Additional info: {AdditionalInfo}";
				return Result.Fail<TResult>(ErrorCode.Unknown, message, popupName, ex.Message);
			}
		}
	}
}
