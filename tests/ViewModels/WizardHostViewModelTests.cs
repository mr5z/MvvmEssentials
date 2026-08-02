using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
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

// The host only invokes the Steps delegates directly; it never calls CreateView itself,
// so this fake only needs to satisfy the interface and track Dispose.
internal sealed class FakeContentViewFactory : IContentViewFactory
{
    public int DisposeCount { get; private set; }

    ContentView IContentViewFactory.CreateView<TContentView, TViewModel>()
        => throw new NotSupportedException("Steps are created via delegates in these tests.");

    void IDisposable.Dispose() => DisposeCount++;
}

// Subclassing exposes the protected members we need to drive/assert against, mirroring
// exactly how a real consumer host VM (e.g. OnboardingHostViewModel) is written.
internal sealed class ExposedWizardHostViewModel(IContentViewFactory viewFactory, int stepCount)
    : WizardHostViewModel<FakeWizardState>(viewFactory)
{
    public int CompletedCount { get; private set; }

    public void PublicOnInitialized() => OnInitialized();
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

    protected override IReadOnlyList<Func<IContentViewFactory, ContentView>> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => new Func<IContentViewFactory, ContentView>(_ => new FakeWizardStepView()))
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
    // CurrentStep / CurrentIndex — raised on every step change
    // -----------------------------------------------------------------------

    [Test]
    public void OnInitialized_RaisesCurrentStepAndCurrentIndexChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);

        // When
        sut.PublicOnInitialized();

        // Then
        Assert.That(_raisedProperties, Does.Contain(nameof(WizardHostViewModel<>.CurrentStep)));
        Assert.That(_raisedProperties, Does.Contain("CurrentIndex"));
    }

    [Test]
    public async Task GoNextAsync_WhenAdvancing_RaisesCurrentIndexChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
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
        sut.PublicOnInitialized();
        await sut.PublicGoNextAsync(); // -> step 1 (last)
        _raisedProperties.Clear();

        // When
        await sut.PublicGoBackAsync(); // -> step 0 (no longer last)

        // Then
        Assert.That(_raisedProperties, Does.Contain("IsLastStep"));
        Assert.That(sut.PublicIsLastStep, Is.False);
    }

    // -----------------------------------------------------------------------
    // No Fody / generator required — plain CLR event subscription is sufficient.
    // Core regression guard for dropping Fody from the library itself.
    // -----------------------------------------------------------------------

    [Test]
    public async Task PropertyChanged_FiresForPlainSubscriberWithoutAnyWeavingOrGenerator()
    {
        // Given — no [ObservableProperty], no [AddINotifyPropertyChangedInterface] anywhere
        // in this test or in WizardHostViewModel; just a plain INotifyPropertyChanged subscriber.
        var sut = CreateSut(stepCount: 2);
        var currentIndexRaisedCount = 0;
        ((INotifyPropertyChanged)sut).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == "CurrentIndex")
                currentIndexRaisedCount++;
        };

        // When
        sut.PublicOnInitialized();
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(currentIndexRaisedCount, Is.EqualTo(2)); // init (index 0) + advance (index 1)
    }
}