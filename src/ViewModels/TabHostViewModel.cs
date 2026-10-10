using System.ComponentModel;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.TabbedPages;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class TabHostViewModel : PageViewModel, ITabHost
{
	private protected override void HandleInitialized()
	{
		base.HandleInitialized();

		// TODO OnTabSelected() gets called twice if SelectedTabIndex != 0
		CurrentTab.OnTabSelected();
	}

	private protected override async Task HandleInitializedAsync()
	{
		await base.HandleInitializedAsync();

		await CurrentTab.OnTabSelectedAsync();
	}

	private protected override void HandleParametersSet(INavigationParameters parameters)
	{
		base.HandleParametersSet(parameters);

		if (parameters.TryGetValue<int>(nameof(SelectedTabIndex), out var selectedTabIndex))
		{
			SelectedTabIndex = selectedTabIndex;
		}
	}

	protected async Task<IResult> SwitchTabAsync<TTabViewModel>(
		INavigationService navigationService,
		INavigationParameters? parameters = null,
		bool animated = true) where TTabViewModel : TabViewModel
	{
		var navParams = parameters ?? new NavigationParameters();
    
		navParams[NavigationHints.IsTabbedPageSwitch] = true;
		var pageName = PageHelper.ToPageName<TTabViewModel>(PagePattern.Page);

		return await navigationService.NavigateAsync(pageName, navParams, animated);
	}

	protected abstract IReadOnlyCollection<ITabComponent> Tabs { get; }

	protected ITabComponent CurrentTab => Tabs.ElementAt(SelectedTabIndex);

	protected int SelectedTabIndex
	{
		get;
		set
		{
			if (field == value)
				return;

			field = value;
			OnPropertyChanged(new PropertyChangedEventArgs(nameof(SelectedTabIndex)));
		}
	}
	
	IReadOnlyCollection<ITabComponent> ITabHost.Tabs => Tabs;
	
	ITabComponent ITabHost.CurrentTab => CurrentTab;

	int ITabHost.SelectedTabIndex
	{
		get => SelectedTabIndex;
		set => SelectedTabIndex = value;
	}
}
