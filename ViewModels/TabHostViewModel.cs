using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabHostViewModel : PageViewModel, ITabHost
{
	protected override void OnInitialized()
	{
		base.OnInitialized();

		// TODO OnTabSelected() gets called twice if SelectedTabIndex != 0
		((IHostComponent)CurrentTab).OnTabSelected();
	}

	protected abstract IReadOnlyCollection<TabViewModel> Tabs { get; }

	protected TabViewModel CurrentTab => Tabs.ElementAt(SelectedTabIndex);

	protected int SelectedTabIndex { get; set; }
	
	IReadOnlyCollection<TabViewModel> ITabHost.Tabs => Tabs;
	
	TabViewModel ITabHost.CurrentTab => CurrentTab;

	int ITabHost.SelectedTabIndex
	{
		get => SelectedTabIndex;
		set => SelectedTabIndex = value;
	}
}
