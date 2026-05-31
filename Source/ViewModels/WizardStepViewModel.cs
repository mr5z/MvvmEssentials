using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

public class WizardStepViewModel<TState> : BaseViewModel, IWizardStep<TState>, IDisposable
{
    void IWizardStep<TState>.OnStepEntered(TState state) => OnStepEntered(state);
    
    TState IWizardStep<TState>.OnStepExited(TState state) => OnStepExited(state);

    protected virtual void OnStepEntered(TState state) { }
    
    protected virtual TState OnStepExited(TState state) => state;
    
    protected virtual void OnDispose() { }
    
#pragma warning disable CA1816
    void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}