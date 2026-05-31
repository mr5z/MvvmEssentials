namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IWizardStep
{
    void OnStepEntered();

    Task OnStepEnteredAsync();
    
    void OnStepExited();
    
    Task OnStepExitedAsync();
}