using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public class WizardStepViewModel : BaseViewModel, IWizardStep, IDisposable
{
    void IWizardStep.OnStepEntered() => OnStepEntered();
    
    Task IWizardStep.OnStepEnteredAsync() => OnStepEnteredAsync();
    
    void IWizardStep.OnStepExited() => OnStepExited();

    Task IWizardStep.OnStepExitedAsync() => OnStepExitedAsync();

    protected virtual void OnStepEntered() { }
    
    protected virtual Task OnStepEnteredAsync() => Task.CompletedTask;

    protected virtual void OnStepExited() { }
    
    protected virtual Task OnStepExitedAsync() => Task.CompletedTask;
    
    protected virtual void OnDispose() { }
    
#pragma warning disable CA1816
    void IDisposable.Dispose() => OnDispose();
#pragma warning restore CA1816
}