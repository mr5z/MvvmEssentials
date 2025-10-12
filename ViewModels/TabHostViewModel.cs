using Nkraft.MvvmEssentials.Services.Navigation;
using System.Collections.Immutable;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabHostViewModel : PageViewModel, ITabHost
{
	public override void OnPageAppearing()
	{
		base.OnPageAppearing();

		// This should have gone through OnInitialized(), but TabbedPage doesn't invoke tha NavigatedTo() lifecycle method.
		SetInitialTabSelected();
	}

	private bool _isInitialTabSet = false;
	private void SetInitialTabSelected()
	{
		if (_isInitialTabSet == false)
		{
			_isInitialTabSet = true;
			CurrentTab.OnTabSelected();
		}
	}

	public abstract ImmutableArray<TabViewModel> GetTabs();

	public TabViewModel CurrentTab => GetTabs().ElementAt(SelectedTabIndex);

	public int SelectedTabIndex { get; set; }
}
