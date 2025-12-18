using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface ITabHost
{
	int SelectedTabIndex { get; set; }

	TabViewModel CurrentTab { get; }

	IReadOnlyCollection<TabViewModel> Tabs { get; }
}
