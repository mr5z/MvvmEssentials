using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

public class WizardStepViewModel<TState> : BaseViewModel, IWizardStep<TState>, IDisposable
{
    void IWizardStep<TState>.OnStepEntered(TState state) => OnStepEntered(state);

    TState IWizardStep<TState>.OnStepExited(TState state) => OnStepExited(state);

    bool IWizardStep<TState>.CanAdvance => CanAdvance;

    Task<bool> IWizardStep<TState>.CanAdvanceAsync() => CanAdvanceAsync();

    protected virtual void OnStepEntered(TState state) { }

    protected virtual TState OnStepExited(TState state) => state;

    /// <summary>
    /// Whether the wizard may move past this step. The host re-reads it when the step is entered and
    /// whenever the step raises <c>PropertyChanged</c>, so read state your view already binds to.
    /// </summary>
    protected virtual bool CanAdvance => true;

    /// <summary>
    /// Asked when Next is invoked and <see cref="CanAdvance"/> allows it, before the step is exited. Return
    /// <c>false</c> to stay, for example after telling the user what's missing. Use it for checks a disabled Next
    /// can't explain, or that need to await; it does not affect <c>CanGoNext</c>.
    /// </summary>
    protected virtual Task<bool> CanAdvanceAsync() => Task.FromResult(true);

    protected virtual void OnDispose() { }

#pragma warning disable CA1816
    void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}
