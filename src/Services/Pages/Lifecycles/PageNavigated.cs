namespace Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

internal interface IPageNavigated
{
    void OnPageNavigatedTo();

    Task OnPageNavigatedToAsync();

    void OnPageNavigatedFrom();

    Task OnPageNavigatedFromAsync();
}

internal interface IRootPageNavigated
{
    void OnNavigatedToRoot(INavigationParameters parameters);
	
    Task OnNavigatedToRootAsync(INavigationParameters parameters);
}