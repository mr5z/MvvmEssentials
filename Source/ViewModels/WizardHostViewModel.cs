using System.Diagnostics.CodeAnalysis;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class WizardHostViewModel(IContentViewFactory viewFactory) : PageViewModel 
{
    private readonly IContentViewFactory _viewFactory = viewFactory;
    private readonly Dictionary<int, ContentView> _stepCache = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        await SetStepAsync(0);
    }

    [SuppressMessage("ReSharper", "MethodHasAsyncOverload")]
    private async Task SetStepAsync(int index)
    {
        CurrentWizardStep.OnStepExited();
        await CurrentWizardStep.OnStepExitedAsync();

        if (_stepCache.TryGetValue(index, out var view))
        {
            CurrentWizardStep.OnStepEntered();
            await CurrentWizardStep.OnStepEnteredAsync();
        }
        else
        {
            view = Steps[index].Invoke(_viewFactory);
            _stepCache[index] = view;
            CurrentWizardStep.OnStepEntered();
            await CurrentWizardStep.OnStepEnteredAsync();
        }

        CurrentIndex = index;
        CurrentStep = view;
    }

    protected virtual bool CanAdvanceFrom(int index) => true;

    protected abstract Task OnCompletedAsync();

    protected async Task GoNextAsync()
    {
        if (CanAdvanceFrom(CurrentIndex) == false)
            return;
        
        if (IsLastStep)
        {
            await OnCompletedAsync();
            return;
        }
        
        await SetStepAsync(CurrentIndex + 1);
    }

    protected async Task GoBackAsync()
    {
        if (CanGoBack)
        {
            await SetStepAsync(CurrentIndex - 1);
        }
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        _viewFactory.Dispose();
        _stepCache.Clear();
    }
    
    protected abstract IReadOnlyList<Func<IContentViewFactory, ContentView>> Steps { get; }

    public ContentView CurrentStep { get; private set; } = null!;

    public IWizardStep CurrentWizardStep => (IWizardStep)CurrentStep.BindingContext;
    
    protected int CurrentIndex { get; private set; }

    protected bool CanGoBack => CurrentIndex > 0;
    
    protected bool IsLastStep => CurrentIndex == Steps.Count - 1;
}
