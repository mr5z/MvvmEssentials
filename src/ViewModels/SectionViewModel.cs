namespace Nkraft.MvvmEssentials.ViewModels;

/// <summary>
/// A bindable part of a page: one area's state and commands, exposed as a property of the page's view model and
/// bound by that area of the page. Register it with <c>IPageRegistry.RegisterSection</c> so it is disposed with
/// its page; override <see cref="OnDispose"/> to release timers and subscriptions.
/// </summary>
public abstract class SectionViewModel : BaseViewModel, IDisposable
{
    protected virtual void OnDispose() { }

#pragma warning disable CA1816
    void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
