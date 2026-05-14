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

	protected abstract IReadOnlyCollection<ITabComponent> Tabs { get; }

	protected ITabComponent CurrentTab => Tabs.ElementAt(SelectedTabIndex);

	protected int SelectedTabIndex { get; set; }
	
	IReadOnlyCollection<ITabComponent> ITabHost.Tabs => Tabs;
	
	ITabComponent ITabHost.CurrentTab => CurrentTab;

	int ITabHost.SelectedTabIndex
	{
		get => SelectedTabIndex;
		set => SelectedTabIndex = value;
	}
}
