using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

// ---------------------------------------------------------------------------
// Fakes (FakeWizardState lives in WizardHostViewModelTests.cs)
// ---------------------------------------------------------------------------

internal sealed class CheckedStepViewModel : WizardStepViewModel<FakeWizardState>
{
    public bool IsReady { get; set; } = true;
    public Func<Task<bool>> Check { get; set; } = () => Task.FromResult(true);
    public int CheckCount { get; private set; }
    public int OnStepExitedCount { get; private set; }

    protected override bool CanAdvance => IsReady;

    protected override Task<bool> CanAdvanceAsync()
    {
        CheckCount++;
        return Check();
    }

    protected override FakeWizardState OnStepExited(FakeWizardState state)
    {
        OnStepExitedCount++;
        return state;
    }
}

internal sealed class CheckedContentViewFactory : IContentViewFactory
{
    public List<CheckedStepViewModel> Created { get; } = [];

    ContentView IContentViewFactory.CreateView<TViewModel>()
    {
        var viewModel = new CheckedStepViewModel();
        Created.Add(viewModel);
        return new ContentView { BindingContext = viewModel };
    }

    void IDisposable.Dispose() { }
}

internal sealed class CheckedWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider? clock = null)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock ?? new FakeTimeProvider())
{
    public int CompletedCount { get; private set; }
    public bool HostAllowsAdvance { get; set; } = true;

    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public void SimulateUnload() => ((IDisposable)this).Dispose();
    public Task PublicGoNextAsync() => GoNextAsync();
    public Task PublicGoBackAsync() => GoBackAsync();
    public int PublicCurrentIndex => CurrentIndex;

    protected override async Task<bool> CanAdvanceFromAsync(int index)
        => HostAllowsAdvance && await base.CanAdvanceFromAsync(index);

    protected override Task OnCompletedAsync()
    {
        CompletedCount++;
        return Task.CompletedTask;
    }

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<CheckedStepViewModel>())
            .ToList();
}

[TestFixture]
public class WizardCanAdvanceAsyncTests
{
    private CheckedContentViewFactory _factory = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CheckedContentViewFactory();
    }

    private CheckedWizardHostViewModel CreateSut(int stepCount)
    {
        var sut = new CheckedWizardHostViewModel(_factory, stepCount);
        sut.SimulateAppearing();
        return sut;
    }

    private CheckedStepViewModel Step(int index) => _factory.Created[index];

    // -----------------------------------------------------------------------
    // Step's CanAdvanceAsync
    // -----------------------------------------------------------------------

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public async Task GoNextAsync_StepCheck_AdvancesOnlyWhenAllowed(bool allowed, int expectedIndex)
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        Step(0).Check = () => Task.FromResult(allowed);

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(expectedIndex));
    }

    [Test]
    public async Task GoNextAsync_StepDeclines_DoesNotExitStep()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        Step(0).Check = () => Task.FromResult(false);

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(Step(0).OnStepExitedCount, Is.EqualTo(0));
    }

    [TestCase(false, 0)]
    [TestCase(true, 1)]
    public async Task GoNextAsync_OnLastStep_CompletesOnlyWhenAllowed(bool allowed, int expectedCompletedCount)
    {
        // Given
        var sut = CreateSut(stepCount: 1);
        Step(0).Check = () => Task.FromResult(allowed);

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.CompletedCount, Is.EqualTo(expectedCompletedCount));
    }

    [Test]
    public async Task GoNextAsync_StepCannotAdvance_DoesNotAskStepCheck()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        Step(0).IsReady = false;

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(Step(0).CheckCount, Is.EqualTo(0));
    }

    [Test]
    public void CanGoNext_StepCheckWouldDecline_IsStillTrue()
    {
        // Given
        var sut = CreateSut(stepCount: 2);

        // When
        Step(0).Check = () => Task.FromResult(false);

        // Then
        Assert.That(sut.CanGoNext, Is.True);
    }

    [Test]
    public async Task GoBackAsync_DoesNotAskStepCheck()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        await sut.PublicGoNextAsync();

        // When
        await sut.PublicGoBackAsync();

        // Then
        Assert.That(Step(1).CheckCount, Is.EqualTo(0));
    }

    // -----------------------------------------------------------------------
    // Host override of CanAdvanceFromAsync
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_HostDeclines_DoesNotAdvance()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        sut.HostAllowsAdvance = false;

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
    }

    // -----------------------------------------------------------------------
    // While the check is pending
    // -----------------------------------------------------------------------

    [Test]
    public async Task GoNextAsync_CalledAgainWhilePending_AdvancesOnce()
    {
        // Given
        var sut = CreateSut(stepCount: 3);
        var pending = new TaskCompletionSource<bool>();
        Step(0).Check = () => pending.Task;
        var first = sut.PublicGoNextAsync();

        // When
        var second = sut.PublicGoNextAsync();
        pending.SetResult(true);
        await Task.WhenAll(first, second);

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
        Assert.That(Step(0).CheckCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GoNextAsync_OnLastStepCalledAgainWhilePending_CompletesOnce()
    {
        // Given
        var sut = CreateSut(stepCount: 1);
        var pending = new TaskCompletionSource<bool>();
        Step(0).Check = () => pending.Task;
        var first = sut.PublicGoNextAsync();

        // When
        var second = sut.PublicGoNextAsync();
        pending.SetResult(true);
        await Task.WhenAll(first, second);

        // Then
        Assert.That(sut.CompletedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GoNextAsync_BackWhilePending_StaysWhereBackWent()
    {
        // Given — on step 1, its check pending
        var sut = CreateSut(stepCount: 3);
        await sut.PublicGoNextAsync();
        var pending = new TaskCompletionSource<bool>();
        Step(1).Check = () => pending.Task;
        var next = sut.PublicGoNextAsync();

        // When
        await sut.PublicGoBackAsync();
        pending.SetResult(true);
        await next;

        // Then — only Back exited step 1
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
        Assert.That(Step(1).OnStepExitedCount, Is.EqualTo(1));
    }

    [Test]
    public async Task GoNextAsync_DisposedWhilePending_DoesNotAdvanceOrComplete()
    {
        // Given
        var sut = CreateSut(stepCount: 1);
        var pending = new TaskCompletionSource<bool>();
        Step(0).Check = () => pending.Task;
        var next = sut.PublicGoNextAsync();

        // When
        sut.SimulateUnload();
        pending.SetResult(true);
        await next;

        // Then
        Assert.That(sut.CompletedCount, Is.EqualTo(0));
        Assert.That(Step(0).OnStepExitedCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GoNextAsync_CheckThrows_LetsNextRunAgain()
    {
        // Given
        var sut = CreateSut(stepCount: 2);
        Step(0).Check = () => Task.FromException<bool>(new InvalidOperationException());
        Assert.ThrowsAsync<InvalidOperationException>(sut.PublicGoNextAsync);

        // When
        Step(0).Check = () => Task.FromResult(true);
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
    }
}
