using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Extensions;

public static class PopupServiceExtension
{
	internal static string ToPopupName<TViewModel>()
	{
		const string knownViewModelPattern = "ViewModel";
		const string knownPopupPattern = "Popup";
		return typeof(TViewModel).Name.Replace(knownViewModelPattern, knownPopupPattern);
	}

	extension(IPopupService popupService)
	{
		public async Task<Result<TResult>> PresentAsync<TViewModel, TResult>(INavigationParameters? parameters = null,
			bool animated = true)
			where TViewModel : IPopupViewModel<TResult>
		{
			var popupName = ToPopupName<TViewModel>();
			var tcs = new TaskCompletionSource<TResult>();
			parameters ??= new NavigationParameters();
			parameters.Add("_completion", tcs);
			var navResult = await popupService.PresentAsync(popupName, parameters, animated);
			if (navResult.IsFailure)
			{
				return Result.Fail<TResult>(ErrorCode.InvalidState, "Failed to display popup '{PopupName}'.", popupName);
			}

			try
			{
				var popupResult = await tcs.Task;
				return Result.Ok(popupResult);
			}
			catch (TaskCanceledException)
			{
				const string error = "Failed to dismiss popup '{PopupName}' after a request for cancellation; Additional info: {AdditionalInfo}";
				var dismissResult = await popupService.DismissAsync(popupName, animated);
				return Result.Fail<TResult>(ErrorCode.Cancelled, error, popupName, dismissResult.ErrorMessage);
			}
			catch (Exception ex)
			{
				const string error = "Failed to dismiss popup '{PopupName}'; Additional info: {AdditionalInfo}";
				return Result.Fail<TResult>(ErrorCode.Unknown, error, popupName, ex.Message);
			}
		}

		public async Task<IResult> DismissAsync<TViewModel>(bool animated = true)
			where TViewModel : IPopupViewModel
		{
			var popupName = ToPopupName<TViewModel>();
			return await popupService.DismissAsync(popupName, animated);
		}
	}
}
