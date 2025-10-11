namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface INavigatedAware
{
	void OnNavigatedTo();

	void OnNavigatedFrom();
}

internal interface INavigatedAwareAsync
{
	Task OnNavigatedToAsync();

	Task OnNavigatedFromAsync();
}

internal interface IRootPageAware
{
	void OnNavigatedToRoot(INavigationParameters parameters);
}

internal interface IRootPageAwareAsync
{
	Task OnNavigatedToRootAsync(INavigationParameters parameters);
}
