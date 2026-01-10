namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IParameterSetAware
{
	void OnParametersSet(INavigationParameters parameters);
}
