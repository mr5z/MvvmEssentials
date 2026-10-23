using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Modals;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.ViewModels;

// ---------------------------------------------------------------------------
// Fakes (FakeWizardState, FakeWizardStepViewModel, FakeContentViewFactory live in WizardHostViewModelTests.cs)
// ---------------------------------------------------------------------------

internal sealed class PresentableWizardHostViewModel(
    IContentViewFactory viewFactory,
    int stepCount,
    TimeProvider? clock = null)
    : WizardHostViewModel<FakeWizardState>(viewFactory, clock ?? new FakeTimeProvider())
{
    public bool AllowDismiss { get; set; } = true;
    public bool GoBackOnBackButton { get; set; } = true;
    public int CanDismissCallCount { get; private set; }
    public IResult? CompletionResult { get; private set; }

    public void SimulateAppearing() => ((IPageAppearing)this).OnPageAppearing();
    public void SimulateUnload() => ((IDisposable)this).Dispose();

    // What ModalService does when Android Back pops a modal it didn't start
    public Task<bool> SimulateBackButton() => ((IModalDismissible)this).Dismiss();

    public Task PublicGoNextAsync() => GoNextAsync();
    public int PublicCurrentIndex => CurrentIndex;
    public bool PublicIsModal => IsModal;

    protected override bool ShouldGoBackOnBackButtonPressed => GoBackOnBackButton;

    protected override Task<bool> CanDismissAsync()
    {
        CanDismissCallCount++;
        return Task.FromResult(AllowDismiss);
    }

    protected override async Task OnCompletedAsync() => CompletionResult = await Dismiss(State);

    protected override IReadOnlyList<WizardStep> Steps { get; }
        = Enumerable.Range(0, stepCount)
            .Select(_ => StepOf<FakeWizardStepViewModel>())
            .ToList();
}

[TestFixture]
public class WizardModalTests
{
    private IModalService _modalService = null!;
    private FakeContentViewFactory _factory = null!;
    private TaskCompletionSource<FakeWizardState> _tcs = null!;

    [SetUp]
    public void SetUp()
    {
        _modalService = Substitute.For<IModalService>();
        _modalService.DismissAsync().Returns(Result.Ok());
        _factory = new FakeContentViewFactory();
        _tcs = new TaskCompletionSource<FakeWizardState>();
    }

    // Parameters as ModalService + PresentAsync<TViewModel, TResult> pass them
    private PresentableWizardHostViewModel CreateModalSut(int stepCount)
    {
        var parameters = new NavigationParameters();
        parameters.Add(NavigationHints.ModalPresenter, _modalService);
        parameters.Add(NavigationHints.ModalCompletionParam, _tcs);
        return CreateSut(stepCount, parameters);
    }

    private PresentableWizardHostViewModel CreateSut(int stepCount, INavigationParameters parameters)
    {
        var sut = new PresentableWizardHostViewModel(_factory, stepCount);
        ((IParametersSet)sut).OnParametersSet(parameters);
        sut.SimulateAppearing();
        return sut;
    }

    // -----------------------------------------------------------------------
    // Pushed, not presented — existing behavior is kept
    // -----------------------------------------------------------------------

    [Test]
    public async Task NotPresentedModally_DismissFailsWithoutTouchingModalService()
    {
        // Given
        var sut = CreateSut(stepCount: 1, new NavigationParameters());

        // When
        var dismissed = await sut.Dismiss();
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicIsModal, Is.False);
        Assert.That(dismissed, Is.False);
        Assert.That(sut.CompletionResult?.IsFailure, Is.True);
        Assert.That(sut.CanDismissCallCount, Is.EqualTo(0));
        await _modalService.DidNotReceive().DismissAsync();
    }

    // -----------------------------------------------------------------------
    // Completion
    // -----------------------------------------------------------------------

    [Test]
    public async Task NextOnLastStep_WhenCompletionDismissesWithState_CompletesCallerTask()
    {
        // Given
        var sut = CreateModalSut(stepCount: 2);
        await sut.PublicGoNextAsync();

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(sut.PublicIsModal, Is.True);
        Assert.That(await _tcs.Task, Is.EqualTo(new FakeWizardState()));
        await _modalService.Received(1).DismissAsync();
    }

    // -----------------------------------------------------------------------
    // Android Back
    // -----------------------------------------------------------------------

    [Test]
    public async Task BackButton_OnLaterStep_GoesToPreviousStepAndKeepsModalOpen()
    {
        // Given
        var sut = CreateModalSut(stepCount: 3);
        await sut.PublicGoNextAsync();

        // When
        var dismissed = await sut.SimulateBackButton();

        // Then
        Assert.That(dismissed, Is.False);
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(0));
        Assert.That(sut.CanDismissCallCount, Is.EqualTo(0));
        await _modalService.DidNotReceive().DismissAsync();
        Assert.That(_tcs.Task.IsCompleted, Is.False);
    }

    [Test]
    public async Task BackButton_OnFirstStep_AsksCanDismissAndCancelsCallerTask()
    {
        // Given
        var sut = CreateModalSut(stepCount: 3);

        // When
        var dismissed = await sut.SimulateBackButton();

        // Then
        Assert.That(dismissed, Is.True);
        Assert.That(sut.CanDismissCallCount, Is.EqualTo(1));
        Assert.That(_tcs.Task.IsCanceled, Is.True);
    }

    [Test]
    public async Task BackButton_WhenGoBackDisabled_DismissesFromLaterStep()
    {
        // Given
        var sut = CreateModalSut(stepCount: 3);
        sut.GoBackOnBackButton = false;
        await sut.PublicGoNextAsync();

        // When
        var dismissed = await sut.SimulateBackButton();

        // Then
        Assert.That(dismissed, Is.True);
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
        Assert.That(_tcs.Task.IsCanceled, Is.True);
    }

    [Test]
    public async Task BackButton_OnFirstStep_WhenCanDismissDeclines_KeepsModalOpen()
    {
        // Given
        var sut = CreateModalSut(stepCount: 2);
        sut.AllowDismiss = false;

        // When
        var dismissed = await sut.SimulateBackButton();

        // Then
        Assert.That(dismissed, Is.False);
        await _modalService.DidNotReceive().DismissAsync();
        Assert.That(_tcs.Task.IsCompleted, Is.False);
    }

    // -----------------------------------------------------------------------
    // Cancel / close command
    // -----------------------------------------------------------------------

    [Test]
    public async Task Dismiss_OnLaterStep_DismissesWithoutSteppingBack()
    {
        // Given
        var sut = CreateModalSut(stepCount: 3);
        await sut.PublicGoNextAsync();

        // When
        var dismissed = await sut.Dismiss();

        // Then
        Assert.That(dismissed, Is.True);
        Assert.That(sut.PublicCurrentIndex, Is.EqualTo(1));
        Assert.That(_tcs.Task.IsCanceled, Is.True);
    }

    // -----------------------------------------------------------------------
    // Dispose — system dismissal (iOS sheet swipe-down)
    // -----------------------------------------------------------------------

    [Test]
    public void Dispose_WithoutDismiss_CancelsCallerTaskAndDisposesSteps()
    {
        // Given
        var sut = CreateModalSut(stepCount: 2);

        // When
        sut.SimulateUnload();

        // Then
        Assert.That(_tcs.Task.IsCanceled, Is.True);
        Assert.That(_factory.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Dispose_DuringCompletionDismissal_DoesNotOverrideResult()
    {
        // Given — MAUI may unload the page before PopModalAsync returns
        var sut = CreateModalSut(stepCount: 1);
        _modalService.DismissAsync().Returns(_ =>
        {
            sut.SimulateUnload();
            return Result.Ok();
        });

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(_tcs.Task.IsCompletedSuccessfully, Is.True);
    }
}
