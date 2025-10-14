using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Extensions;

public static class PopupServiceExtension
{
	internal static string ToPopupName<TViewModel>()
	{
		const string KnownViewModelPattern = "ViewModel";
		const string KnownPopupPattern = "Popup";
		return typeof(TViewModel).Name.Replace(KnownViewModelPattern, KnownPopupPattern);
	}

	public static async Task<Result<TResult>> ShowAsync<TViewModel, TResult>(
		this IPopupService popupService,
		INavigationParameters? parameters = null,
		bool animated = true)
		where TViewModel : IPopupViewModel<TResult>
	{
		var popupName = ToPopupName<TViewModel>();
		var tcs = new TaskCompletionSource<TResult>();
		parameters ??= new NavigationParameters();
		parameters.Add("_completion", tcs);
		var navResult = await popupService.ShowAsync(popupName, parameters, animated);
		if (navResult.IsFailure)
		{
			return Result.Fail<TResult>(ErrorCode.InvalidState, "Failed to display popup '{PopupName}'.", popupName);
		}

		try
		{
			var popupResult = await tcs.Task;
			return Result.Ok(popupResult);
		}
		catch (Exception ex)
		{
			var dismissResult = await popupService.DismissAsync(popupName, animated);
			if (dismissResult.IsSuccess)
			{
				return Result.Fail<TResult>(ErrorCode.General, ex.Message);
			}
			else
			{
				const string error = "Failed to dismiss popup after a request for cancellation. {AdditionalInfo}.";
				return Result.Fail<TResult>(ErrorCode.InvalidState, error, dismissResult.ErrorMessage);
			}
		}
	}

	public static async Task<IResult> DismissAsync<TViewModel>(
		this IPopupService popupService, 
		bool animated = true)
		where TViewModel : PageViewModel
		// Oh come on!
		//where TViewModel : PopupViewModel<>
	{
		var popupName = ToPopupName<TViewModel>();
		return await popupService.DismissAsync(popupName, animated);
	}
}
