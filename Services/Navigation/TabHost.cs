namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface ITabHost
{
	int SelectedTabIndex { get; set; }

	ITabComponent CurrentTab { get; }

	IReadOnlyCollection<ITabComponent> Tabs { get; }
}
