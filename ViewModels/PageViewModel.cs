using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public class PageViewModel : BaseViewModel,
	IAppearingAware,
	IPageLoadAware,
	IWindowEventAware,
	IWindowEventAwareAsync
{
	public virtual void OnPageAppearing() { }

	public virtual void OnPageDisappearing() { }

	public virtual void OnPageUnloaded() { }

	public virtual void OnWindowActivated() { }

	public virtual Task OnWindowActivatedAsync() => Task.CompletedTask;
}
