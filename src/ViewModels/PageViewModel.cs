using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

namespace Nkraft.MvvmEssentials.ViewModels;

public class PageViewModel : NavigableEntryViewModel,
	IPageAppearing,
	IPageNavigated,
	IPageLoad,
	IDisposable
{

	private bool _isInitialized = false;
	protected virtual void OnPageAppearing()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
	}

	private bool _isInitializedAsync = false;
	protected virtual async Task OnPageAppearingAsync() 
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
	}

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
	
	void IPageAppearing.OnPageAppearing() => OnPageAppearing();
	
	void IPageAppearing.OnPageDisappearing() => OnPageDisappearing();
	
	Task IPageAppearing.OnPageAppearingAsync() => OnPageAppearingAsync();
	
	Task IPageAppearing.OnPageDisappearingAsync() => Task.CompletedTask;
	
	void IPageNavigated.OnPageNavigatedTo() => OnNavigatedTo();
	
	void IPageNavigated.OnPageNavigatedFrom() => OnNavigatedFrom();
	
	void IPageLoad.OnPageUnloaded() => OnPageUnloaded();
	
#pragma warning disable CA1816
	void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
