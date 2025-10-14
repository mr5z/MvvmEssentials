using Nkraft.MvvmEssentials.ViewModels;
using System.Collections.Immutable;

namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface ITabHost
{
	int SelectedTabIndex { get; set; }

	TabViewModel CurrentTab { get; }

	ImmutableArray<TabViewModel> GetTabs();
}
