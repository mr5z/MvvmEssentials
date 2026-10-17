using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class WizardHostViewModel<TState>(IContentViewFactory viewFactory) : PageViewModel where TState : new()
{
    private readonly IContentViewFactory _viewFactory = viewFactory;
    private readonly Dictionary<int, ContentView> _stepCache = [];

    private protected override void HandleInitialized()
    {
        base.HandleInitialized();

        SetStep(0);
    }

    private protected override void HandleDispose()
    {
        base.HandleDispose();

        foreach (var view in _stepCache.Values)
        {
            UnregisterEventHandler(view);
        }

        _viewFactory.Dispose();
        _stepCache.Clear();
    }

    private void SetStep(int index)
    {
        State = CommitCurrentStep(State);

        if (_stepCache.TryGetValue(index, out var view) == false)
        {
            view = Steps[index].Create(_viewFactory);
            RegisterEventHandler(view);
            _stepCache[index] = view;
        }

        CurrentIndex = index;
        CurrentStep = view;

        var incoming = (IWizardStep<TState>)view.BindingContext;
        incoming.OnStepEntered(State);

        // After OnStepEntered, which may restore the step's state without raising PropertyChanged
        UpdateCanGoNext();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);

        if (args.PropertyName != nameof(CanGoNext))
        {
            UpdateCanGoNext();
        }
    }

    private void Step_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == CurrentStep?.BindingContext)
        {
            UpdateCanGoNext();
        }
    }

    private void RegisterEventHandler(ContentView view)
    {
        ((INotifyPropertyChanged)view.BindingContext).PropertyChanged += Step_PropertyChanged;
    }

    private void UnregisterEventHandler(ContentView view)
    {
        ((INotifyPropertyChanged)view.BindingContext).PropertyChanged -= Step_PropertyChanged;
    }

    private void UpdateCanGoNext() => CanGoNext = CurrentStep is not null && CanAdvanceFrom(CurrentIndex);

    /// <summary>
    /// Whether the wizard may move past the step at <paramref name="index"/>, or complete if it is the last.
    /// Defaults to the step's <c>CanAdvance</c>. <see cref="CanGoNext"/> is re-read whenever the host or the
    /// current step raises <c>PropertyChanged</c>.
    /// </summary>
    protected virtual bool CanAdvanceFrom(int index)
    {
        if (_stepCache.TryGetValue(index, out var view) == false)
            return true;

        return ((IWizardStep<TState>)view.BindingContext).CanAdvance;
    }

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

    public ContentView? CurrentStep
    {
        get;
        private set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentStep)));
        }
    }

    public int CurrentIndex
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
    } = -1; // To trigger INPC by SetStep(0) from OnInitialized()

    public bool CanGoBack => CurrentIndex > 0;

    public bool IsLastStep => CurrentIndex == Steps.Count - 1;

    /// <summary>Whether Next can proceed from the current step. <c>false</c> until the first step is entered.</summary>
    public bool CanGoNext
    {
        get;
        private set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CanGoNext)));
        }
    }

    protected TState State { get; set; } = new();

    /// <summary>
    /// The wizard's steps, in order. Build each entry with <see cref="StepOf{TViewModel}"/>.
    /// </summary>
    protected abstract IReadOnlyList<WizardStep> Steps { get; }

    /// <summary>
    /// Declares a step without creating it. The step's view and ViewModel are created the first time the
    /// wizard enters it, then cached. The view is the one mapped via
    /// <see cref="IPageRegistry.MapView{TView,TViewModel}"/>.
    /// </summary>
    protected static WizardStep StepOf<TViewModel>() where TViewModel : WizardStepViewModel<TState>
        => new(factory => factory.CreateView<TViewModel>());

    /// <summary>A wizard step that is created on first visit. Create one with <see cref="WizardHostViewModel{TState}.StepOf{TViewModel}"/>.</summary>
    protected sealed class WizardStep
    {
        private readonly Func<IContentViewFactory, ContentView> _create;

        internal WizardStep(Func<IContentViewFactory, ContentView> create) => _create = create;

        internal ContentView Create(IContentViewFactory factory) => _create(factory);
    }
}
