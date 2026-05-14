using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public class PageViewModel : NavigableEntryViewModel,
	IAppearingAware,
	IAppearingAwareAsync,
	INavigatedAware,
	IPageLoadAware,
	IDisposable
{
	private bool _isInitialized = false;
	private bool _isInitializedAsync = false;

	protected virtual void OnPageAppearing()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
	}

	protected virtual void OnPageDisappearing() { }

	protected virtual async Task OnPageAppearingAsync() 
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
	}

	protected virtual Task OnPageDisappearingAsync() => Task.CompletedTask;

	protected virtual void OnNavigatedTo() { }

	protected virtual Task OnNavigatedToAsync() => Task.CompletedTask;

	protected virtual void OnNavigatedFrom() { }

	protected virtual Task OnNavigatedFromAsync() => Task.CompletedTask;

	protected virtual void OnPageUnloaded() { }

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;

	protected virtual void OnDispose() { }
	
	void IAppearingAware.OnPageAppearing() => OnPageAppearing();
	
	void IAppearingAware.OnPageDisappearing() => OnPageDisappearing();
	
	Task IAppearingAwareAsync.OnPageAppearingAsync() => OnPageAppearingAsync();
	
	Task IAppearingAwareAsync.OnPageDisappearingAsync() => Task.CompletedTask;
	
	void INavigatedAware.OnNavigatedTo() => OnNavigatedTo();
	
	void INavigatedAware.OnNavigatedFrom() => OnNavigatedFrom();
	
	void IPageLoadAware.OnPageUnloaded() => OnPageUnloaded();
	
#pragma warning disable CA1816
	void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
