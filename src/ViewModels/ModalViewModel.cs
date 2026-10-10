using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public interface IModalViewModel;

public interface IModalViewModel<TResult> : IModalViewModel;

public partial class ModalViewModel<TResult>(IModalService modalService) : PageViewModel, IModalViewModel<TResult>
{
	private readonly IModalService _modalService = modalService;

	private TaskCompletionSource<TResult>? _completion;
	
	// Set while Dismiss() is popping this modal, so the resulting unload isn't mistaken for a system dismissal
	private bool _isDismissing;

	private protected override void HandleParametersSet(INavigationParameters parameters)
	{
		base.HandleParametersSet(parameters);

		if (parameters.TryGetValue<TaskCompletionSource<TResult>>(NavigationHints.PopupCompletionParam, out var completion))
		{
			_completion = completion;
		}
	}

	private protected override void HandleDispose()
	{
		base.HandleDispose();

		// Disposed without going through Dismiss(): Android back button, iOS sheet swipe-down
		if (_isDismissing == false)
		{
			_completion?.TrySetCanceled();
		}
	}

	[RelayCommand]
	public async Task<bool> Dismiss()
	{
		var result = await DismissModalAsync();
		if (result.IsSuccess)
		{
			_completion?.TrySetCanceled();
		}
		return result.IsSuccess;
	}

	protected async Task<IResult> Dismiss(TResult result)
	{
		var modalResult = await DismissModalAsync();
		if (modalResult.IsSuccess)
		{
			_completion?.TrySetResult(result);
		}
		else
		{
			_completion?.TrySetException(new Exception(modalResult.ErrorMessage));
		}

		return modalResult;
	}

	private async Task<IResult> DismissModalAsync()
	{
		_isDismissing = true;

		try
		{
			return await _modalService.DismissAsync();
		}
		finally
		{
			_isDismissing = false;
		}
	}
}