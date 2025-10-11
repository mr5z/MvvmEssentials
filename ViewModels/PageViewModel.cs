using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public class PageViewModel : BaseViewModel,
	IAppearingAware,
	INavigatedAware,
	INavigatedAwareAsync,
	IPageLoadAware,
	IWindowEventAware,
	IWindowEventAwareAsync
{
	private bool _isInitialized = false;
	private bool _isInitializedAsync = false;

	// TODO abstract this away
	void INavigatedAware.OnNavigatedFrom() { }

	// TODO abstract this away
	Task INavigatedAwareAsync.OnNavigatedFromAsync() => Task.CompletedTask;

	void INavigatedAware.OnNavigatedTo() 
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
	}

	async Task INavigatedAwareAsync.OnNavigatedToAsync()
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
	}

	public virtual void OnPageAppearing() { }

	public virtual void OnPageDisappearing() { }

	public virtual void OnPageUnloaded() { }

	public virtual void OnWindowActivated() { }

	public virtual Task OnWindowActivatedAsync() => Task.CompletedTask;

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;
}
