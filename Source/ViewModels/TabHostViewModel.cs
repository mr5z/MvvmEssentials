using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.TabbedPages;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabHostViewModel : PageViewModel, ITabHost
{
	protected override void OnInitialized()
	{
		base.OnInitialized();

		// TODO OnTabSelected() gets called twice if SelectedTabIndex != 0
		CurrentTab.OnTabSelected();
	}

	protected override async Task OnInitializedAsync()
	{
		await base.OnInitializedAsync();
		
		await CurrentTab.OnTabSelectedAsync();
	}

	protected async Task<IResult> SwitchTabAsync<TTabViewModel>(
		INavigationService navigationService,
		INavigationParameters? parameters = null,
		bool animated = true) where TTabViewModel : TabViewModel
	{
		var navParams = parameters ?? new NavigationParameters();
    
		navParams[NavigationHints.IsTabbedPageSwitch] = true;
		var pageName = PageHelper.ToPageName<TTabViewModel>("Page");

		return await navigationService.NavigateAsync(pageName, navParams, animated);
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
