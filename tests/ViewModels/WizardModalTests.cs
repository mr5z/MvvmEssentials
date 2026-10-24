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
    private IModalPresenter _presenter = null!;
    private FakeContentViewFactory _factory = null!;
    private ModalPresentation<FakeWizardState> _presentation = null!;

    [SetUp]
    public void SetUp()
    {
        _presenter = Substitute.For<IModalPresenter>();
        _presenter.DismissAsync(Arg.Any<ModalPresentation>()).Returns(Result.Ok());
        _factory = new FakeContentViewFactory();
        _presentation = new ModalPresentation<FakeWizardState>();
        _presentation.Open(_presenter, new ContentPage(), new ContentPage());
    }

    // Parameters as ModalService + PresentAsync<TViewModel, TResult> pass them
    private PresentableWizardHostViewModel CreateModalSut(int stepCount)
    {
        var parameters = new NavigationParameters { { NavigationHints.ModalPresentation, _presentation } };
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
        await _presenter.DidNotReceive().DismissAsync(Arg.Any<ModalPresentation>());
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
        Assert.That(await _presentation.Result, Is.EqualTo(new FakeWizardState()));
        await _presenter.Received(1).DismissAsync(Arg.Any<ModalPresentation>());
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
        await _presenter.DidNotReceive().DismissAsync(Arg.Any<ModalPresentation>());
        Assert.That(_presentation.Result.IsCompleted, Is.False);
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
        Assert.That(_presentation.Result.IsCanceled, Is.True);
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
        Assert.That(_presentation.Result.IsCanceled, Is.True);
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
        await _presenter.DidNotReceive().DismissAsync(Arg.Any<ModalPresentation>());
        Assert.That(_presentation.Result.IsCompleted, Is.False);
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
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    // -----------------------------------------------------------------------
    // Dispose — the modal closing, not the view model, completes the caller
    // -----------------------------------------------------------------------

    [Test]
    public void Dispose_WithoutDismiss_DisposesStepsAndLeavesTheCallerToTheModal()
    {
        // Given
        var sut = CreateModalSut(stepCount: 2);

        // When
        sut.SimulateUnload();

        // Then
        Assert.That(_presentation.Result.IsCompleted, Is.False);
        Assert.That(_factory.DisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void ModalClosedBySystem_CancelsTheCaller()
    {
        // Given: iOS sheet swipe-down; ModalService reports MAUI's ModalPopped
        CreateModalSut(stepCount: 2);

        // When
        _presentation.OnClosed();

        // Then
        Assert.That(_presentation.Result.IsCanceled, Is.True);
    }

    [Test]
    public async Task Dispose_DuringCompletionDismissal_DoesNotOverrideResult()
    {
        // Given — MAUI may unload the page before PopModalAsync returns
        var sut = CreateModalSut(stepCount: 1);
        _presenter.DismissAsync(Arg.Any<ModalPresentation>()).Returns(_ =>
        {
            sut.SimulateUnload();
            return Result.Ok();
        });

        // When
        await sut.PublicGoNextAsync();

        // Then
        Assert.That(_presentation.Result.IsCompletedSuccessfully, Is.True);
    }
}
