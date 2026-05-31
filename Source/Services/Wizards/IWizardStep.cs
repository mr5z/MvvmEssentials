namespace Nkraft.MvvmEssentials.Services.Wizards;

internal interface IWizardStep<TState>
{
    void OnStepEntered(TState state);

    Task OnStepEnteredAsync(TState state);
    
    TState OnStepExited(TState state);
    
    Task<TState> OnStepExitedAsync(TState state);
}