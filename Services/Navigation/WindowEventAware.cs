namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IWindowEventAware
{
	void OnWindowActivated();
}

internal interface IWindowEventAwareAsync
{
	Task OnWindowActivatedAsync();
}
