using System.ComponentModel;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

// ---------------------------------------------------------------------------
// Fakes (FakeWizardState lives in WizardHostViewModelTests.cs)
// ---------------------------------------------------------------------------

internal sealed class GatedStepViewModel : WizardStepViewModel<FakeWizardState>
{
    public bool IsReady { get; private set; }

    protected override bool CanAdvance => IsReady;

    public void SetReady(bool value)
    {
        IsReady = value;
        Raise(nameof(IsReady));
    }

    public void SetReadySilently(bool value) => IsReady = value;

    public void Raise(string? propertyName) => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
}

internal sealed class GatedContentViewFactory : IContentViewFactory
{
    public List<GatedStepViewModel> Created { get; } = [];

    ContentView IContentViewFactory.CreateView<TViewModel>()
    {
        var viewModel = new GatedStepViewModel();
        Created.Add(viewModel);
        return new ContentView { BindingContext = viewModel };
    }

    void IDisposable.Dispose() { }
}

internal sealed class GatedWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider? clock = null)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock ?? new FakeTimeProvider())
{
    public int CompletedCount { get; private set; }
    public bool HostAllowsAdvance
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HostAllowsAdvance)));
        }
    } = true;

    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public Task PublicGoNextAsync() => GoNextAsync();
    public Task PublicGoBackAsync() => GoBackAsync();
    public int PublicCurrentIndex => CurrentIndex;

    protected override bool CanAdvanceFrom(int index) => HostAllowsAdvance && base.CanAdvanceFrom(index);

    protected override Task OnCompletedAsync()
    {
        CompletedCount++;
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<GatedStepViewModel>())
            .ToList();
}

[TestFixture]
public class WizardCanAdvanceTests
{
    private GatedContentViewFactory _factory = null!;
    private int _canGoNextChangedCount;

    [SetUp]
    public void SetUp()
    {
        _factory = new GatedContentViewFactory();
        _canGoNextChangedCount = 0;
    }

    private GatedWizardHostViewModel CreateSut(int stepCount)
    {
        var sut = new GatedWizardHostViewModel(_factory, stepCount);
        ((INotifyPropertyChanged)sut).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WizardHostViewModel<>.CanGoNext))
                _canGoNextChangedCount++;
        };
        return sut;
    }

    private GatedStepViewModel Step(int index) => _factory.Created[index];

    // -----------------------------------------------------------------------
    // Initial value
    // -----------------------------------------------------------------------

    [Test]
    public void CanGoNext_BeforeFirstStep_IsFalse()
    {
        var sut = CreateSut(stepCount: 2);

        Assert.That(sut.CanGoNext, Is.False);
    }

    [Test]
    public void OnInitialized_StepNotReady_DoesNotRaiseCanGoNextChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);

        // When
        sut.SimulateAppearing();

        // Then
        Assert.That(sut.CanGoNext, Is.False);
        Assert.That(_canGoNextChangedCount, Is.EqualTo(0));
    }

    [Test]
    public void OnInitialized_StepWithoutOverride_CanGoNextIsTrue()
    {
        // Given — FakeWizardStepViewModel does not override CanAdvance
        var sut = new ExposedWizardHostViewModel(new FakeContentViewFactory(), stepCount: 2);

        // When
        sut.SimulateAppearing();

        // Then
        Assert.That(sut.CanGoNext, Is.True);
    }

    // -----------------------------------------------------------------------
    // Current step raises PropertyChanged — any name
    // -----------------------------------------------------------------------

    [TestCase("IsReady")]
    [TestCase("Ns.IAnswered.IsAnswered")] // name of an explicit interface implementation
    [TestCase("")]
    [TestCase(null)]
    public void StepPropertyChanged_AnyName_RaisesCanGoNextChanged(string? propertyName)
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        Step(0).SetReadySilently(true);

        // When
        Step(0).Raise(propertyName);

        // Then
        Assert.That(sut.CanGoNext, Is.True);
        Assert.That(_canGoNextChangedCount, Is.EqualTo(1));
    }

    [Test]
    public void StepPropertyChanged_ValueUnchanged_DoesNotRaiseCanGoNextChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        Step(0).SetReady(true);
        _canGoNextChangedCount = 0;

        // When
        Step(0).Raise("Other");

        // Then
        Assert.That(_canGoNextChangedCount, Is.EqualTo(0));
    }

    [Test]
    public async Task NonCurrentStepPropertyChanged_DoesNotRaiseCanGoNextChanged()
    {
        // Given — on step 1; step 0 stays cached and subscribed
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        Step(0).SetReady(true);
        await sut.PublicGoNextAsync();
        _canGoNextChangedCount = 0;

        // When
        Step(0).SetReady(false);

        // Then
        Assert.That(sut.CanGoNext, Is.False);
        Assert.That(_canGoNextChangedCount, Is.EqualTo(0));
    }

    // -----------------------------------------------------------------------
    // Step changes
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_IntoStepNotReady_RaisesCanGoNextChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        sut.SimulateAppearing();
        Step(0).SetReady(true);
        _canGoNextChangedCount = 0;

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.CanGoNext, Is.False);
        Assert.That(_canGoNextChangedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GoBackAsync_ToStepChangedSilently_ReadsCurrentValue()
    {
        // Given — step 0 becomes not ready while off-screen, without raising PropertyChanged
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        Step(0).SetReady(true);
        await sut.PublicGoNextAsync();
        Step(1).SetReady(true);
        Step(0).SetReadySilently(false);

        // When
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(sut.CanGoNext, Is.False);
    }

    // -----------------------------------------------------------------------
    // GoNextAsync gating
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_StepNotReady_DoesNotAdvance()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
    }

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public async Task GoNextAsync_OnLastStep_CompletesOnlyWhenReady(bool ready, int expectedCompletedCount)
    {
        // Given
        var sut = CreateSut(stepCount: 1);
        sut.SimulateAppearing();
        Step(0).SetReady(ready);

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.CompletedCount, Is.EqualTo(expectedCompletedCount));
    }

    // -----------------------------------------------------------------------
    // Host override of CanAdvanceFrom
    // -----------------------------------------------------------------------

    [Test]
    public void HostPropertyChanged_ReadByOverride_RaisesCanGoNextChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        Step(0).SetReady(true);
        _canGoNextChangedCount = 0;

        // When
        sut.HostAllowsAdvance = false;

        // Then
        Assert.That(sut.CanGoNext, Is.False);
        Assert.That(_canGoNextChangedCount, Is.EqualTo(1));
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    [Test]
    public void StepPropertyChanged_AfterDispose_DoesNotRaiseCanGoNextChanged()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.SimulateAppearing();
        var step = Step(0);
        ((IDisposable)sut).Dispose();

        // When
        step.SetReady(true);

        // Then
        Assert.That(_canGoNextChangedCount, Is.EqualTo(0));
    }
}
