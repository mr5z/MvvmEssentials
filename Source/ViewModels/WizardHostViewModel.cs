using System.Diagnostics.CodeAnalysis;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class WizardHostViewModel<TState>(IContentViewFactory viewFactory) : PageViewModel where TState : new()
{
    private readonly IContentViewFactory _viewFactory = viewFactory;
    private readonly Dictionary<int, ContentView> _stepCache = [];
    
    protected TState State { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        await SetStepAsync(0);
    }

    [SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "It is by design.")]
    private async Task SetStepAsync(int index)
    {
        State = await CommitCurrentStepAsync(State);

        if (_stepCache.TryGetValue(index, out var view) == false)
        {
            view = Steps[index].Invoke(_viewFactory);
            _stepCache[index] = view;
        }

        CurrentIndex = index;
        CurrentStep = view;

        var incoming = (IWizardStep<TState>)view.BindingContext;
        incoming.OnStepEntered(State);
        await incoming.OnStepEnteredAsync(State);
    }

    protected virtual bool CanAdvanceFrom(int index) => true;

    protected abstract Task OnCompletedAsync();

    protected async Task GoNextAsync()
    {
        if (CanAdvanceFrom(CurrentIndex) == false)
            return;
        
        if (IsLastStep)
        {
            State = await CommitCurrentStepAsync(State);
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
    
    [SuppressMessage("ReSharper", "MethodHasAsyncOverload", Justification = "It is by design.")]
    private async Task<TState> CommitCurrentStepAsync(TState state)
    {
        if (CurrentStep?.BindingContext is IWizardStep<TState> outgoing)
        {
            var newState = outgoing.OnStepExited(state);
            newState = await outgoing.OnStepExitedAsync(newState);
            return newState;
        }
        return state;
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        _viewFactory.Dispose();
        _stepCache.Clear();
    }
    
    protected abstract IReadOnlyList<Func<IContentViewFactory, ContentView>> Steps { get; }

    public ContentView? CurrentStep { get; private set; }

    protected int CurrentIndex { get; private set; }

    protected bool CanGoBack => CurrentIndex > 0;
    
    protected bool IsLastStep => CurrentIndex == Steps.Count - 1;
}
