using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

namespace Nkraft.MvvmEssentials.ViewModels;

public class PageViewModel : NavigableEntryViewModel,
	IPageAppearing,
	IPageNavigated,
	IPageLoad,
	IDisposable
{
	private bool _isInitialized = false;
	private void HandlePageAppearing()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
		
		OnPageAppearing();
	}

	private bool _isInitializedAsync = false;
	private async Task HandlePageAppearingAsync() 
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
		
		await OnPageAppearingAsync();
	}
	
	protected virtual void OnPageAppearing() { }

	protected virtual Task OnPageAppearingAsync() => Task.CompletedTask;

	protected virtual void OnPageDisappearing() { }

	protected virtual Task OnPageDisappearingAsync() => Task.CompletedTask;

	protected virtual void OnNavigatedTo() { }

	protected virtual Task OnNavigatedToAsync() => Task.CompletedTask;

	protected virtual void OnNavigatedFrom() { }

	protected virtual Task OnNavigatedFromAsync() => Task.CompletedTask;

	protected virtual void OnPageUnloaded() { }

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;

	protected virtual void OnDispose() { }
	
	void IPageAppearing.OnPageAppearing() => HandlePageAppearing();
	
	Task IPageAppearing.OnPageAppearingAsync() => HandlePageAppearingAsync();
	
	void IPageAppearing.OnPageDisappearing() => OnPageDisappearing();
	
	Task IPageAppearing.OnPageDisappearingAsync() => Task.CompletedTask;
	
	void IPageNavigated.OnPageNavigatedTo() => OnNavigatedTo();
	
	void IPageNavigated.OnPageNavigatedFrom() => OnNavigatedFrom();
	
	void IPageLoad.OnPageUnloaded() => OnPageUnloaded();
	
#pragma warning disable CA1816
	void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
