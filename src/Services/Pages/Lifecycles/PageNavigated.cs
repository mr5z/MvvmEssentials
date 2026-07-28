namespace Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

internal interface IPageNavigated
{
	void OnPageNavigatedTo();

	void OnPageNavigatedFrom();
}

internal interface IRootPageNavigated
{
	void OnNavigatedToRoot(INavigationParameters parameters);
	
	Task OnNavigatedToRootAsync(INavigationParameters parameters);
}
