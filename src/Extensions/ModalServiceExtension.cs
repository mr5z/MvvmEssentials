using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.ViewModels;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

public static class ModalServiceExtension
{
	extension(IModalService modalService)
	{
		/// <summary>
		/// Presents the destination produced by a generated <c>With(...)</c> factory modally, without awaiting a result.
		/// </summary>
		public Task<IResult> PresentAsync(PageDestination destination, bool animated = true)
			=> modalService.PresentAsync(destination.PageName, destination.Parameters, animated);

		/// <summary>
		/// Presents the page associated with <typeparamref name="TViewModel"/> modally, without awaiting a result.
		/// </summary>
		public Task<IResult> PresentAsync<TViewModel>(INavigationParameters? parameters = null, bool animated = true)
			where TViewModel : PageViewModel
			=> modalService.PresentAsync(PageHelper.ToPageName<TViewModel>(PagePattern.Page), parameters, animated);

		/// <summary>
		/// Presents the page associated with <typeparamref name="TViewModel"/> modally and awaits its result.
		/// </summary>
		/// <returns>The result passed to <c>Dismiss(TResult)</c>, or <see cref="ErrorCode.Cancelled"/> if the modal was dismissed without one.</returns>
		public async Task<Result<TResult>> PresentAsync<TViewModel, TResult>(
			INavigationParameters? parameters = null, bool animated = true)
			where TViewModel : IModalViewModel<TResult>
			=> await IModalService.PresentAsync<TResult>(
				modalService,
				PageHelper.ToPageName<TViewModel>(PagePattern.Page),
				parameters ?? new NavigationParameters(),
				animated);

		private static async Task<Result<TResult>> PresentAsync<TResult>(
			IModalService modal, string modalName, INavigationParameters parameters, bool animated)
		{
			var tcs = new TaskCompletionSource<TResult>();
			parameters[NavigationHints.PopupCompletionParam] = tcs;

			var navResult = await modal.PresentAsync(modalName, parameters, animated);
			if (navResult.IsFailure)
				return Result.Fail<TResult>(ErrorCode.InvalidState, "Failed to present modal '{ModalName}'.", modalName);

			try
			{
				var modalResult = await tcs.Task;
				return Result.Ok(modalResult);
			}
			catch (TaskCanceledException)
			{
				// Unlike popups, a cancelled modal is already gone: either Dismiss() popped it or the system did.
				// Dismissing here would pop whichever modal is now on top.
				const string message = "Modal '{ModalName}' has been cancelled.";
				return Result.Fail<TResult>(ErrorCode.Cancelled, message, modalName);
			}
			catch (Exception ex)
			{
				const string message = "Failed to dismiss modal '{ModalName}'; Additional info: {AdditionalInfo}";
				return Result.Fail<TResult>(ErrorCode.Unknown, message, modalName, ex.Message);
			}
		}
	}
}