namespace Nkraft.MvvmEssentials.Services.Navigation;

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
