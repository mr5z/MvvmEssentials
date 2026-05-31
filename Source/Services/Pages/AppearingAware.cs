namespace Nkraft.MvvmEssentials.Services.Pages;

internal interface IAppearingAware
{
	void OnPageAppearing();

	void OnPageDisappearing();
}

internal interface IAppearingAwareAsync
{
	Task OnPageAppearingAsync();

	Task OnPageDisappearingAsync();
}
