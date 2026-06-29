namespace Nkraft.MvvmEssentials.Services.Pages;

internal interface IPageAppearingAware
{
	void OnPageAppearing();

	void OnPageDisappearing();
}

internal interface IPageAppearingAwareAsync
{
	Task OnPageAppearingAsync();

	Task OnPageDisappearingAsync();
}
