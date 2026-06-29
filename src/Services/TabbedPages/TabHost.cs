namespace Nkraft.MvvmEssentials.Services.TabbedPages;

internal interface ITabHost
{
	int SelectedTabIndex { get; set; }

	ITabComponent CurrentTab { get; }

	IReadOnlyCollection<ITabComponent> Tabs { get; }
}
