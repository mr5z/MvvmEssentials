using Nkraft.MvvmEssentials.Services.Navigation;
using System.Collections.Immutable;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabHostViewModel : PageViewModel, ITabHost
{
	protected override void OnInitialized()
	{
		base.OnInitialized();

		// TODO OnTabSelected() gets called twice if SelectedTabIndex != 0
		CurrentTab.OnTabSelected();
	}

	public abstract ImmutableArray<TabViewModel> GetTabs();

	public TabViewModel CurrentTab => GetTabs().ElementAt(SelectedTabIndex);

	public int SelectedTabIndex { get; set; }
}
