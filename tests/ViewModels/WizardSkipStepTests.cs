using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

// ---------------------------------------------------------------------------
// Fakes (FakeWizardState lives in WizardHostViewModelTests.cs)
// ---------------------------------------------------------------------------

internal sealed class AnsweringStepViewModel : WizardStepViewModel<FakeWizardState>
{
    public int Answer { get; set; }
    public int OnStepEnteredCount { get; private set; }
    public int OnStepExitedCount { get; private set; }

    protected override void OnStepEntered(FakeWizardState state) => OnStepEnteredCount++;

    protected override FakeWizardState OnStepExited(FakeWizardState state)
    {
        OnStepExitedCount++;
        return state with { Value = Answer };
    }
}

internal sealed class AnsweringContentViewFactory : IContentViewFactory
{
    public List<AnsweringStepViewModel> Created { get; } = [];

    ContentView IContentViewFactory.CreateView<TViewModel>()
    {
        var viewModel = new AnsweringStepViewModel();
        Created.Add(viewModel);
        return new ContentView { BindingContext = viewModel };
    }

    void IDisposable.Dispose() { }
}

internal sealed class SkippingWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider? clock = null)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock ?? new FakeTimeProvider())
{
    public int CompletedCount { get; private set; }
    public List<int> AskedIndices { get; } = [];

    // Which steps to skip, given the index and the committed state
    public Func<int, FakeWizardState, bool> Skip
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Skip)));
        }
    } = (_, _) => false;

    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public Task PublicGoNextAsync() => GoNextAsync();
    public Task PublicGoBackAsync() => GoBackAsync();
    public int PublicCurrentIndex => CurrentIndex;
    public FakeWizardState PublicState => State;

    protected override bool ShouldSkipStep(int index)
    {
        AskedIndices.Add(index);
        return Skip(index, State);
    }

    protected override Task OnCompletedAsync()
    {
        CompletedCount++;
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<AnsweringStepViewModel>())
            .ToList();
}

[TestFixture]
public class WizardSkipStepTests
{
    private AnsweringContentViewFactory _factory = null!;
    private List<string> _raisedProperties = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new AnsweringContentViewFactory();
        _raisedProperties = [];
    }

    private SkippingWizardHostViewModel CreateSut(int stepCount)
    {
        var sut = new SkippingWizardHostViewModel(_factory, stepCount);
        ((INotifyPropertyChanged)sut).PropertyChanged += (_, e) => _raisedProperties.Add(e.PropertyName!);
        return sut;
    }

    // -----------------------------------------------------------------------
    // GoNextAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_OverSkippedStep_EntersStepAfterItWithoutCreatingSkipped()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (index, _) => index == 1;
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(2));
        Assert.That(_factory.Created, Has.Count.EqualTo(2));
        Assert.That(_factory.Created[1].OnStepEnteredCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GoNextAsync_SkipDecidedByOutgoingStep_ReadsStateItJustCommitted()
    {
        // Given — step 1 is skipped only once step 0 has answered 1
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (index, state) => index == 1 && state.Value == 1;
        sut.SimulateAppearing();
        _factory.Created[0].Answer = 1;

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(2));
    }

    [Test]
    public async Task GoNextAsync_RemainingStepsSkipped_CommitsOnceThenCompletes()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (index, _) => index > 0;
        sut.SimulateAppearing();
        _factory.Created[0].Answer = 7;

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.CompletedCount, Is.EqualTo(1));
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
        Assert.That(_factory.Created[0].OnStepExitedCount, Is.EqualTo(1));
        Assert.That(sut.PublicState.Value, Is.EqualTo(7));
    }

    [Test]
    public async Task GoNextAsync_MovingToNextStep_ExitsOutgoingStepOnce()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (index, _) => index == 1;
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(_factory.Created[0].OnStepExitedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // GoBackAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoBackAsync_OverSkippedStep_EntersStepBeforeIt()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (index, _) => index == 1;
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync();

        // When
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
        Assert.That(_factory.Created[0].OnStepEnteredCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GoBackAsync_StepSkippedAfterVisit_PassesOverCachedStep()
    {
        // Given — step 1 was visited, then skipped by the time the wizard comes back
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync();
        await sut.PublicGoNextAsync();
        var step1 = _factory.Created[1];
        sut.Skip = (index, _) => index == 1;

        // When
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
        Assert.That(step1.OnStepEnteredCount, Is.EqualTo(1));
        Assert.That(step1.OnStepExitedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // First step
    // -----------------------------------------------------------------------

    [Test]
    public async Task ShouldSkipStep_IsNeverAskedForFirstStep()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (_, _) => true;
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.AskedIndices, Does.Not.Contain(0));
        Assert.That(sut.CompletedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // IsLastStep
    // -----------------------------------------------------------------------

    [Test]
    public void IsLastStep_WhenLaterStepsSkipped_IsTrueOnEntry()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.Skip = (index, _) => index > 0;

        // When
        sut.SimulateAppearing();

        // Then
        Assert.That(sut.IsLastStep, Is.True);
        Assert.That(_raisedProperties, Does.Contain(nameof(WizardHostViewModel<>.IsLastStep)));
    }

    [Test]
    public void IsLastStep_HostPropertyChangesSkip_RaisesIsLastStepChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        _raisedProperties.Clear();

        // When
        sut.Skip = (index, _) => index == 1;

        // Then
        Assert.That(sut.IsLastStep, Is.True);
        Assert.That(_raisedProperties, Does.Contain(nameof(WizardHostViewModel<>.IsLastStep)));
    }

    [Test]
    public async Task IsLastStep_OnStepBeforeOnlySkippedSteps_IsTrue()
    {
        // Given
        var sut = CreateSut(stepCount: 4);
        sut.Skip = (index, _) => index == 3;
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(2));
        Assert.That(sut.IsLastStep, Is.True);
    }
}
