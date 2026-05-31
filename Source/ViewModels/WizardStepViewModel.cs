using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

public class WizardStepViewModel<TState> : BaseViewModel, IWizardStep<TState>, IDisposable
{
    void IWizardStep<TState>.OnStepEntered(TState state) => OnStepEntered(state);
    
    Task IWizardStep<TState>.OnStepEnteredAsync(TState state) => OnStepEnteredAsync(state);
    
    TState IWizardStep<TState>.OnStepExited(TState state) => OnStepExited(state);

    Task<TState> IWizardStep<TState>.OnStepExitedAsync(TState state) => OnStepExitedAsync(state);

    protected virtual void OnStepEntered(TState state) { }
    
    protected virtual Task OnStepEnteredAsync(TState state) => Task.CompletedTask;

    protected virtual TState OnStepExited(TState state) => state;
    
    protected virtual Task<TState> OnStepExitedAsync(TState state) => Task.FromResult(state);
    
    protected virtual void OnDispose() { }
    
#pragma warning disable CA1816
    void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}