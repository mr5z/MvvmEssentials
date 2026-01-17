using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabViewModel : BaseViewModel, IHostComponent
{
	private bool _isInitialized;
	private bool _isInitializedAsync;

	protected virtual void OnTabSelected()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
	}

	protected virtual async Task OnTabSelectedAsync()
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
	}

	protected virtual void OnTabUnselected() { }
	
	protected virtual Task OnTabUnselectedAsync() => Task.CompletedTask;

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;
	
	void IHostComponent.OnTabSelected() => OnTabSelected();
	
	void IHostComponent.OnTabUnselected() => OnTabUnselected();
	
	Task IHostComponent.OnTabSelectedAsync() => OnTabSelectedAsync();
	
	Task IHostComponent.OnTabUnselectedAsync() => OnTabUnselectedAsync();
}
