using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Wizards;

namespace Nkraft.MvvmEssentials.ViewModels;

/// <summary>
/// Hosts a multi-step flow in one page. Push it like any page, or present it with
/// <c>IModalService.PresentAsync&lt;THostViewModel, TState&gt;()</c> and hand the final state back with
/// <see cref="Dismiss(TState)"/>.
/// </summary>
public abstract partial class WizardHostViewModel<TState>(IContentViewFactory viewFactory)
    : PageViewModel, IModalViewModel<TState>, IModalDismissible where TState : new()
{
    private readonly IContentViewFactory _viewFactory = viewFactory;
    private readonly Dictionary<int, ContentView> _stepCache = [];

    // Set only when ModalService presented this page
    private ModalSession<TState>? _modalSession;

    private protected override void HandleParametersSet(INavigationParameters parameters)
    {
        base.HandleParametersSet(parameters);

        if (parameters.TryGetValue<IModalService>(NavigationHints.ModalPresenter, out var modalService))
        {
            _modalSession = new ModalSession<TState>(modalService);
            _modalSession.Attach(parameters);
        }
    }

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

        // Disposed without going through Dismiss(): Android back button, iOS sheet swipe-down
        _modalSession?.NotifyDisposed();
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

    /// <summary>Whether <see cref="IModalService"/> presented this wizard, so it can be left with <c>Dismiss</c>.</summary>
    protected bool IsModal => _modalSession is not null;

    /// <summary>
    /// Modal only. Asked before the wizard is dismissed without a result: the <see cref="Dismiss()"/> command
    /// (Cancel / close) and the Android Back button when it isn't going to a previous step.
    /// Return <c>false</c> to keep the wizard open. Not called for <see cref="Dismiss(TState)"/> or the iOS
    /// sheet swipe-down gesture.
    /// </summary>
    protected virtual Task<bool> CanDismissAsync() => Task.FromResult(true);

    /// <summary>
    /// Modal only. When <c>true</c> (default), the Android Back button goes to the previous step while
    /// <see cref="CanGoBack"/> is <c>true</c>, and dismisses only from the first step.
    /// </summary>
    protected virtual bool ShouldGoBackOnBackButtonPressed => true;

    /// <summary>
    /// Modal only. Dismisses without a result (Cancel / close) from any step; the caller receives
    /// <c>ErrorCode.Cancelled</c>. Returns <c>false</c> if not presented modally, <see cref="CanDismissAsync"/>
    /// declined, or the modal could not be dismissed.
    /// </summary>
    [RelayCommand]
    public Task<bool> Dismiss()
        => _modalSession?.DismissAsync(CanDismissAsync) ?? Task.FromResult(false);

    /// <summary>
    /// Modal only. Dismisses the wizard and hands <paramref name="state"/> to the caller; call it from
    /// <see cref="OnCompletedAsync"/>, usually with <see cref="State"/>. Fails with <c>ErrorCode.InvalidState</c>
    /// if the wizard was not presented modally.
    /// </summary>
    protected Task<IResult> Dismiss(TState state)
    {
        if (_modalSession is null)
        {
            const string message = "The wizard was not presented with IModalService.";
            return Task.FromResult<IResult>(Result.Fail(ErrorCode.InvalidState, message));
        }

        return _modalSession.DismissAsync(state);
    }

    // Android Back on a modal; the Cancel / close command calls Dismiss() directly.
    async Task<bool> IModalDismissible.Dismiss()
    {
        if (ShouldGoBackOnBackButtonPressed && CanGoBack)
        {
            await GoBackAsync();
            return false;
        }

        return await Dismiss();
    }

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
