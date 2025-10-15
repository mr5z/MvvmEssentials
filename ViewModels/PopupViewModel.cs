using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IPopupViewModel { }

public interface IPopupViewModel<TResult> : IPopupViewModel { }

public partial class PopupViewModel<TResult>(IPopupService popupService) : PageViewModel, IPopupViewModel<TResult>, IPopupDismissible
{
	private readonly IPopupService _popupService = popupService;

	private TaskCompletionSource<TResult>? _completion;

	public override void OnParametersSet(INavigationParameters parameters)
	{
		base.OnParametersSet(parameters);

		if (parameters.TryGetValue<TaskCompletionSource<TResult>>("_completion", out var completion))
		{
			_completion = completion;
		}
	}

	[RelayCommand]
	public async Task<bool> Dismiss()
	{
		var result = await _popupService.DismissAsync();
		if (result.IsSuccess)
		{
			_completion?.TrySetCanceled();
		}
		return result.IsSuccess;
	}

	protected async Task<IResult> Dismiss(TResult result)
	{
		var popupResult = await _popupService.DismissAsync();
		if (popupResult.IsSuccess)
		{
			_completion?.TrySetResult(result);
		}
		else
		{
			_completion?.TrySetException(new Exception(popupResult.ErrorMessage));
		}

		return popupResult;
	}

	// I don't actually know what this means
	public virtual bool ShouldHandleBackButtonPressed => false;

	public virtual bool ShouldDismissOnBackgroundClicked => true;

	void IPopupDismissible.NotifyCancellation()
	{
		_completion?.TrySetCanceled();
		if (_popupService is PopupService service)
		{
			// TODO wtf is this garbage?
			service.RemovePopupDueToCancellation(PageName);
		}
	}

	protected override string PageName => TypeName.Replace("ViewModel", "Popup");
}
