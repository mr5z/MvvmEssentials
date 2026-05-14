using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabViewModel : BaseViewModel, ITabComponent, IDisposable
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
	
	protected virtual void OnDispose() { }
	
	void ITabComponent.OnTabSelected() => OnTabSelected();
	
	void ITabComponent.OnTabUnselected() => OnTabUnselected();
	
	Task ITabComponent.OnTabSelectedAsync() => OnTabSelectedAsync();
	
	Task ITabComponent.OnTabUnselectedAsync() => OnTabUnselectedAsync();
	
#pragma warning disable CA1816
	void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
