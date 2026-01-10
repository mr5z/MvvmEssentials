namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabViewModel : BaseViewModel
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

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;
}
