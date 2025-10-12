using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public class TabViewModel : BaseViewModel, IRootPageAware, IRootPageAwareAsync
{
	private bool _isInitialized;

	public virtual async void OnTabSelected()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
			await OnInitializedAsync();
		}
	}

	public virtual void OnTabUnselected() { }

	public virtual void OnNavigatedToRoot(INavigationParameters parameters) { }

	public virtual Task OnNavigatedToRootAsync(INavigationParameters parameters) => Task.CompletedTask;
}
