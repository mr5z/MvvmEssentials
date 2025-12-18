using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public class PageViewModel : NavigableEntryViewModel,
	IAppearingAware,
	IAppearingAwareAsync,
	INavigatedAware,
	IPageLoadAware,
	IWindowEventAware,
	IWindowEventAwareAsync
{
	private bool _isInitialized = false;
	private bool _isInitializedAsync = false;

	// TODO abstract this away
	public virtual void OnNavigatedTo() { }

	// TODO abstract this away
	public virtual Task OnNavigatedToAsync() => Task.CompletedTask;

	// TODO abstract this away
	public virtual void OnNavigatedFrom() { }

	// TODO abstract this away
	public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;

	public virtual void OnPageAppearing()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
	}

	public virtual void OnPageDisappearing() { }

	public virtual async Task OnPageAppearingAsync() 
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
	}

	public virtual Task OnPageDisappearingAsync() => Task.CompletedTask;

	public virtual void OnPageUnloaded() { }

	public virtual void OnWindowActivated() { }

	public virtual Task OnWindowActivatedAsync() => Task.CompletedTask;

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;
}
