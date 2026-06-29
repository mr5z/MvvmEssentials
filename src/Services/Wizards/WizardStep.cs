namespace Nkraft.MvvmEssentials.Services.Wizards;

internal interface IWizardStep<TState>
{
    void OnStepEntered(TState state);

    TState OnStepExited(TState state);
}