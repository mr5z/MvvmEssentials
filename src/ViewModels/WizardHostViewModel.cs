using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class WizardHostViewModel<TState>(IContentViewFactory viewFactory) : PageViewModel where TState : new()
{
    private readonly IContentViewFactory _viewFactory = viewFactory;
    private readonly Dictionary<int, ContentView> _stepCache = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        SetStep(0);
    }

    private void SetStep(int index)
    {
        State = CommitCurrentStep(State);

        if (_stepCache.TryGetValue(index, out var view) == false)
        {
            view = Steps[index].Invoke(_viewFactory);
            _stepCache[index] = view;
        }

        CurrentIndex = index;
        CurrentStep = view;

        var incoming = (IWizardStep<TState>)view.BindingContext;
        incoming.OnStepEntered(State);
    }

    protected virtual bool CanAdvanceFrom(int index) => true;

    protected abstract Task OnCompletedAsync();

    protected async Task GoNextAsync()
    {
        if (CanAdvanceFrom(CurrentIndex) == false)
            return;
        
        if (IsLastStep)
        {
            State = CommitCurrentStep(State);
            await OnCompletedAsync();
            return;
        }
        
        SetStep(CurrentIndex + 1);
    }

    protected Task GoBackAsync()
    {
        if (CanGoBack)
        {
            SetStep(CurrentIndex - 1);
        }
        
        return Task.CompletedTask;
    }
    
    private TState CommitCurrentStep(TState state)
    {
        if (CurrentStep?.BindingContext is IWizardStep<TState> outgoing)
        {
            return outgoing.OnStepExited(state);
        }
        return state;
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        _viewFactory.Dispose();
        _stepCache.Clear();
    }

    public ContentView? CurrentStep
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentStep)));
        }
    }

    protected int CurrentIndex
    {
        get;
        private set
        {
            if (field == value) 
                return;
            
            var wasFirst = CanGoBack == false;
            var wasLast = IsLastStep;
            field = value;
            
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentIndex)));
            if (wasFirst != (CanGoBack == false))
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(CanGoBack)));
            if (wasLast != IsLastStep)
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsLastStep)));
        }
    }

    protected bool CanGoBack => CurrentIndex > 0;
    
    protected bool IsLastStep => CurrentIndex == Steps.Count - 1;

    protected TState State { get; set; } = new();
    
    protected abstract IReadOnlyList<Func<IContentViewFactory, ContentView>> Steps { get; }
    
    
}
