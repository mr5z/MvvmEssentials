using Nkraft.MvvmEssentials.Services;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabViewModel : BaseViewModel, ITabComponent, IDisposable
{
	private bool _isInitialized;
	private void HandleTabSelected()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
		OnTabSelected();
	}

	private bool _isInitializedAsync;
	private async Task HandleTabSelectedAsync()
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
		await OnTabSelectedAsync();
	}
	
	protected virtual void OnTabSelected() { }

	protected virtual Task OnTabSelectedAsync() => Task.CompletedTask;

	protected virtual void OnTabUnselected() { }
	
	protected virtual Task OnTabUnselectedAsync() => Task.CompletedTask;

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;
	
	protected virtual void OnDispose() { }
	
	void ITabComponent.OnTabSelected() => HandleTabSelected();
	
	void ITabComponent.OnTabUnselected() => OnTabUnselected();
	
	Task ITabComponent.OnTabSelectedAsync() => HandleTabSelectedAsync();
	
	Task ITabComponent.OnTabUnselectedAsync() => OnTabUnselectedAsync();
	
#pragma warning disable CA1816
	void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
