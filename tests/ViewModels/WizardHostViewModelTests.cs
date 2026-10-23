using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

// ---------------------------------------------------------------------------
// Fakes
// ---------------------------------------------------------------------------

internal sealed record FakeWizardState
{
    public int Value { get; init; }
}

// Moves AutoAdvance past on every read, so back-to-back Next calls in a test are never taken for a double tap.
// Set AutoAdvance to zero and call Advance to test the repeated-tap window itself.
internal sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public TimeSpan AutoAdvance { get; set; } = TimeSpan.FromSeconds(1);

    public void Advance(TimeSpan by) => _now += by;

    public override DateTimeOffset GetUtcNow()
    {
        var now = _now;
        _now += AutoAdvance;
        return now;
    }
}

internal sealed class FakeWizardStepViewModel : WizardStepViewModel<FakeWizardState>
{
    public int OnStepEnteredCount { get; private set; }
    public int OnStepExitedCount { get; private set; }

    protected override void OnStepEntered(FakeWizardState state) => OnStepEnteredCount++;

    protected override FakeWizardState OnStepExited(FakeWizardState state)
    {
        OnStepExitedCount++;
        return state;
    }
}

internal sealed class FakeWizardStepView : ContentView
{
    public FakeWizardStepView() => BindingContext = new FakeWizardStepViewModel();
}

// Steps go through Defer<T>(), which calls the factory. FakeWizardStepView sets its own
// BindingContext, so no DI is needed here.
internal sealed class FakeContentViewFactory : IContentViewFactory
{
    public int DisposeCount { get; private set; }
    public int CreateCount { get; private set; }

    ContentView IContentViewFactory.CreateView<TViewModel>()
    {
        CreateCount++;
        return new FakeWizardStepView();
    }

    void IDisposable.Dispose() => DisposeCount++;
}

// Subclassing exposes the protected members we need to drive/assert against, mirroring
// exactly how a real consumer host VM (e.g. OnboardingHostViewModel) is written.
internal sealed class ExposedWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider? clock = null)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock ?? new FakeTimeProvider())
{
    public int CompletedCount { get; private set; }
    
    // Goes through the real lifecycle path (HandleInitialized → OnInitialized), as PageFactory does
    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public Task PublicGoNextAsync() => GoNextAsync();
    public Task PublicGoBackAsync() => GoBackAsync();
    public bool PublicCanGoBack => CanGoBack;
    public bool PublicIsLastStep => IsLastStep;
    public int PublicCurrentIndex => CurrentIndex;

    protected override Task OnCompletedAsync()
    {
        CompletedCount++;
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<FakeWizardStepViewModel>())
            .ToList();
}

[TestFixture]
public class WizardHostViewModelTests
{
    private FakeContentViewFactory _factory = null!;
    private List<string> _raisedProperties = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new FakeContentViewFactory();
        _raisedProperties = [];
    }

    private ExposedWizardHostViewModel CreateSut(int stepCount)
    {
        var sut = new ExposedWizardHostViewModel(_factory, stepCount);
        ((INotifyPropertyChanged)sut).PropertyChanged += (_, e) => _raisedProperties.Add(e.PropertyName!);
        return sut;
    }

    // -----------------------------------------------------------------------
    // Defer — steps are created on first visit, then cached
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_CreatesOnlyTheFirstStep()
    {
        // Given
        var sut = CreateSut(stepCount: 3);

        // When
        sut.SimulateAppearing();

        // Then
        Assert.That(_factory.CreateCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GoBackThenNext_ReusesCachedSteps()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync();

        // When
        await sut.PublicGoBackAsync();
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(_factory.CreateCount, Is.EqualTo(2));
    }

    // -----------------------------------------------------------------------
    // CurrentStep / CurrentIndex — raised on every step change
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_RaisesCurrentStepAndCurrentIndexChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);

        // When
        sut.SimulateAppearing();

        // Then
        Assert.That(_raisedProperties, Does.Contain(nameof(WizardHostViewModel<>.CurrentStep)));
        Assert.That(_raisedProperties, Does.Contain("CurrentIndex"));
    }

    [Test]
    public async Task GoNextAsync_WhenAdvancing_RaisesCurrentIndexChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        _raisedProperties.Clear();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(_raisedProperties, Does.Contain("CurrentIndex"));
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task GoBackAsync_AlreadyAtFirstStep_DoesNotRaiseAnything()
    {
        // Given — GoBackAsync no-ops entirely when CanGoBack is false
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        _raisedProperties.Clear();

        // When
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(_raisedProperties, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // CanGoBack — only raised when it actually flips
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_FromFirstStep_RaisesCanGoBackChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        _raisedProperties.Clear();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(_raisedProperties, Does.Contain("CanGoBack"));
        Assert.That(sut.PublicCanGoBack, Is.True);
    }

    [Test]
    public async Task GoNextAsync_BetweenMiddleSteps_DoesNotRaiseCanGoBackChanged()
    {
        // Given — CanGoBack is already true at step 1; moving to step 2 shouldn't flip it
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync(); // step 0 -> 1
        _raisedProperties.Clear();

        // When
        await sut.PublicGoNextAsync(); // step 1 -> 2

        // Then
        Assert.That(_raisedProperties, Does.Not.Contain("CanGoBack"));
    }

    [Test]
    public async Task GoBackAsync_ToFirstStep_RaisesCanGoBackChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync(); // step 0 -> 1
        _raisedProperties.Clear();

        // When
        await sut.PublicGoBackAsync(); // step 1 -> 0

        // Then
        Assert.That(_raisedProperties, Does.Contain("CanGoBack"));
        Assert.That(sut.PublicCanGoBack, Is.False);
    }

    // -----------------------------------------------------------------------
    // IsLastStep — only raised when it actually flips
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_ToLastStep_RaisesIsLastStepChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        _raisedProperties.Clear();

        // When
        await sut.PublicGoNextAsync(); // step 0 -> 1 (last)

        // Then
        Assert.That(_raisedProperties, Does.Contain("IsLastStep"));
        Assert.That(sut.PublicIsLastStep, Is.True);
    }

    [Test]
    public async Task GoNextAsync_NotReachingLastStep_DoesNotRaiseIsLastStepChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        _raisedProperties.Clear();

        // When
        await sut.PublicGoNextAsync(); // step 0 -> 1, still not last (last is index 2)

        // Then
        Assert.That(_raisedProperties, Does.Not.Contain("IsLastStep"));
    }

    [Test]
    public async Task GoBackAsync_LeavingLastStep_RaisesIsLastStepChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync(); // -> step 1 (last)
        _raisedProperties.Clear();

        // When
        await sut.PublicGoBackAsync(); // -> step 0 (no longer last)

        // Then
        Assert.That(_raisedProperties, Does.Contain("IsLastStep"));
        Assert.That(sut.PublicIsLastStep, Is.False);
    }

    [Test]
    public async Task PropertyChanged_FiresForPlainSubscriberWithoutAnyWeavingOrGenerator()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        var currentIndexRaisedCount = 0;
        ((INotifyPropertyChanged)sut).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "CurrentIndex")
                currentIndexRaisedCount++;
        };

        // When
        sut.SimulateAppearing();
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(currentIndexRaisedCount, Is.EqualTo(2)); // init (index 0) + advance (index 1)
    }
}