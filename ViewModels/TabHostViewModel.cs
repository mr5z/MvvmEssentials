using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabHostViewModel : PageViewModel, ITabHost
{
	protected override void OnInitialized()
	{
		base.OnInitialized();

		// TODO OnTabSelected() gets called twice if SelectedTabIndex != 0
		CurrentTab.OnTabSelected();
	}

	public abstract IReadOnlyCollection<TabViewModel> Tabs { get; }

	public TabViewModel CurrentTab => Tabs.ElementAt(SelectedTabIndex);

	public int SelectedTabIndex { get; set; }
}
