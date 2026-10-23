using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

// ---------------------------------------------------------------------------
// Fakes (CheckedStepViewModel, CheckedContentViewFactory and CheckedWizardHostViewModel live in
// WizardCanAdvanceAsyncTests.cs; FakeTimeProvider in WizardHostViewModelTests.cs)
// ---------------------------------------------------------------------------

internal sealed class CompletingWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider clock)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock)
{
    public int CompletedCount { get; private set; }
    public TaskCompletionSource Completion { get; } = new();

    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public Task PublicGoNextAsync() => GoNextAsync();

    protected override Task OnCompletedAsync()
    {
        CompletedCount++;
        return Completion.Task;
    }

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<CheckedStepViewModel>())
            .ToList();
}

internal sealed class UnguardedWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider clock)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock)
{
    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public Task PublicGoNextAsync() => GoNextAsync();
    public int PublicCurrentIndex => CurrentIndex;

    protected override TimeSpan RepeatedTapWindow => TimeSpan.Zero;

    protected override Task OnCompletedAsync() => Task.CompletedTask;

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<CheckedStepViewModel>())
            .ToList();
}

// A tap that lands on a step the user hasn't seen yet is a bounce, a double tap, or one tap delivered twice.
// The wizard ignores Next and Back for a short window after every step change.
[TestFixture]
public class WizardRepeatedTapTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(300);

    private CheckedContentViewFactory _factory = null!;
    private FakeTimeProvider _clock = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CheckedContentViewFactory();
        _clock = new FakeTimeProvider { AutoAdvance = TimeSpan.Zero };
    }

    private CheckedWizardHostViewModel CreateSut(int stepCount)
    {
        var sut = new CheckedWizardHostViewModel(_factory, stepCount, _clock);
        sut.SimulateAppearing();
        _clock.Advance(Window);
        return sut;
    }

    private CheckedStepViewModel Step(int index) => _factory.Created[index];

    // The bug: step 0's check passes without awaiting, so the whole Next finishes inside the first click.
    // A second click from the same tap then ran on step 1, whose check (an alert in the app) fired at once.
    [Test]
    public async Task GoNextAsync_RepeatedWithinWindow_DoesNotCheckTheStepJustEntered()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        await sut.PublicGoNextAsync();
        Step(1).Check = () => Task.FromResult(false);

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.Multiple(() =>
        {
            Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
            Assert.That(Step(1).CheckCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task GoNextAsync_RepeatedWithinWindow_AdvancesOnce()
    {
        // Given
        var sut = CreateSut(stepCount: 3);

        // When
        await sut.PublicGoNextAsync();
        await sut.PublicGoNextAsync();

        // Then
        Assert.Multiple(() =>
        {
            Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
            Assert.That(Step(1).OnStepExitedCount, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task GoNextAsync_AfterWindow_ChecksTheStepJustEntered()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        await sut.PublicGoNextAsync();
        Step(1).Check = () => Task.FromResult(false);
        _clock.Advance(Window);

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.Multiple(() =>
        {
            Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
            Assert.That(Step(1).CheckCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task GoNextAsync_JustBeforeWindowEnds_IsStillIgnored()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        await sut.PublicGoNextAsync();
        _clock.Advance(Window - TimeSpan.FromMilliseconds(1));

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task GoNextAsync_RightAfterAppearing_IsIgnored()
    {
        // Given
        var sut = new CheckedWizardHostViewModel(_factory, stepCount: 2, _clock);
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
    }

    [Test]
    public async Task GoBackAsync_RepeatedWithinWindow_GoesBackOnce()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        await sut.PublicGoNextAsync();
        _clock.Advance(Window);
        await sut.PublicGoNextAsync();
        _clock.Advance(Window);

        // When
        await sut.PublicGoBackAsync();
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task GoBackAsync_RightAfterNext_IsIgnored()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        await sut.PublicGoNextAsync();

        // When
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
    }

    [Test]
    public async Task GoNextAsync_RepeatedWithWindowTurnedOff_Advances()
    {
        // Given
        var sut = new UnguardedWizardHostViewModel(_factory, stepCount: 3, _clock);
        sut.SimulateAppearing();

        // When
        await sut.PublicGoNextAsync();
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(2));
    }

    [Test]
    public async Task GoNextAsync_WhileCompleting_CompletesOnce()
    {
        // Given
        var sut = new CompletingWizardHostViewModel(_factory, stepCount: 1, _clock);
        sut.SimulateAppearing();
        _clock.Advance(Window);
        var first = sut.PublicGoNextAsync();
        _clock.Advance(Window);

        // When
        await sut.PublicGoNextAsync();
        sut.Completion.SetResult();
        await first;

        // Then
        Assert.Multiple(() =>
        {
            Assert.That(sut.CompletedCount, Is.EqualTo(1));
            Assert.That(Step(0).OnStepExitedCount, Is.EqualTo(1));
        });
    }
}
