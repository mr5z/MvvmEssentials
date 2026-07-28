namespace Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

internal interface IPageAppearing
{
	void OnPageAppearing();
	
	Task OnPageAppearingAsync();

	void OnPageDisappearing();

	Task OnPageDisappearingAsync();
}