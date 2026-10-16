using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Dispatchers;

[TestFixture]
public class LifecycleExceptionHandlerTests
{
    private IDispatcher _dispatcher = null!;
    private Action? _dispatchedAction;

    [SetUp]
    public void SetUp()
    {
        _dispatchedAction = null;
        _dispatcher = Substitute.For<IDispatcher>();
        _dispatcher.Dispatch(Arg.Any<Action>()).Returns(call =>
        {
            _dispatchedAction = call.Arg<Action>();
            return true;
        });
    }

    private LifecycleExceptionHandler CreateSut(Action<MvvmEssentialsOptions>? configure = null)
    {
        var options = new MvvmEssentialsOptions();
        configure?.Invoke(options);
        return new LifecycleExceptionHandler(
            NullLogger<LifecycleExceptionHandler>.Instance,
            _dispatcher,
            Options.Create(options));
    }

    // -----------------------------------------------------------------------
    // Behavior
    // -----------------------------------------------------------------------

    [Test]
    public void Options_DefaultBehavior_IsCrash()
    {
        Assert.That(new MvvmEssentialsOptions().LifecycleExceptionBehavior, Is.EqualTo(LifecycleExceptionBehavior.Crash));
    }

    [Test]
    public void Handle_WhenCrash_DoesNotThrowSynchronously()
    {
        // Given
        var sut = CreateSut();

        // When / Then — the caller is usually inside MAUI's navigation code
        Assert.DoesNotThrow(() => sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing"));
    }

    [Test]
    public void Handle_WhenCrash_RethrowsOriginalExceptionOnDispatcher()
    {
        // Given
        var sut = CreateSut();
        var exception = new InvalidOperationException("boom");

        // When
        sut.Handle(exception, "OnPageAppearing");

        // Then
        Assert.That(_dispatchedAction, Is.Not.Null, "crash was not dispatched");
        var thrown = Assert.Throws<InvalidOperationException>(() => _dispatchedAction!());
        Assert.That(thrown, Is.SameAs(exception));
    }

    [Test]
    public void Handle_WhenLogOnly_DoesNotDispatch()
    {
        // Given
        var sut = CreateSut(o => o.LifecycleExceptionBehavior = LifecycleExceptionBehavior.LogOnly);

        // When
        sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing");

        // Then
        _dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Test]
    public void Handle_WhenDispatchFails_DoesNotThrow()
    {
        // Given — dispatcher no longer accepts work (e.g. app shutting down)
        _dispatcher.Dispatch(Arg.Any<Action>()).Returns(false);
        var sut = CreateSut();

        // When / Then
        Assert.DoesNotThrow(() => sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing"));
    }

    // -----------------------------------------------------------------------
    // Callback
    // -----------------------------------------------------------------------

    [Test]
    public void Handle_WithCallback_ReceivesExceptionMethodAndSource()
    {
        // Given
        LifecycleExceptionContext? received = null;
        var sut = CreateSut(o => o.OnLifecycleException = ctx => received = ctx);
        var exception = new InvalidOperationException("boom");
        var source = new object();

        // When
        sut.Handle(exception, "OnPageAppearing", source);

        // Then
        Assert.That(received, Is.Not.Null);
        Assert.That(received!.Exception, Is.SameAs(exception));
        Assert.That(received.MethodName, Is.EqualTo("OnPageAppearing"));
        Assert.That(received.Source, Is.SameAs(source));
        Assert.That(received.Handled, Is.False);
    }

    [Test]
    public void Handle_WithCallback_IsCalledBeforeCrashIsDispatched()
    {
        // Given
        var dispatchedWhenCallbackRan = true;
        var sut = CreateSut(o => o.OnLifecycleException = _ => dispatchedWhenCallbackRan = _dispatchedAction is not null);

        // When
        sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing");

        // Then
        Assert.That(dispatchedWhenCallbackRan, Is.False);
        Assert.That(_dispatchedAction, Is.Not.Null);
    }

    [Test]
    public void Handle_WhenCallbackMarksHandled_DoesNotCrash()
    {
        // Given
        var sut = CreateSut(o => o.OnLifecycleException = ctx => ctx.Handled = true);

        // When
        sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing");

        // Then
        _dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Test]
    public void Handle_WhenLogOnly_StillCallsCallback()
    {
        // Given
        var called = false;
        var sut = CreateSut(o =>
        {
            o.LifecycleExceptionBehavior = LifecycleExceptionBehavior.LogOnly;
            o.OnLifecycleException = _ => called = true;
        });

        // When
        sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing");

        // Then
        Assert.That(called, Is.True);
    }

    [Test]
    public void Handle_WhenCallbackThrows_DoesNotEscapeAndStillCrashes()
    {
        // Given
        var sut = CreateSut(o => o.OnLifecycleException = _ => throw new InvalidOperationException("callback boom"));

        // When / Then
        Assert.DoesNotThrow(() => sut.Handle(new InvalidOperationException("boom"), "OnPageAppearing"));
        Assert.That(_dispatchedAction, Is.Not.Null);
    }

    // -----------------------------------------------------------------------
    // Invoke / InvokeAsync extensions
    // -----------------------------------------------------------------------

    [Test]
    public void Invoke_WhenActionThrows_DoesNotEscapeAndReports()
    {
        // Given
        var handler = Substitute.For<ILifecycleExceptionHandler>();
        var exception = new InvalidOperationException("boom");
        var source = new object();

        // When / Then
        Assert.DoesNotThrow(() => handler.Invoke(() => throw exception, "OnPageAppearing", source));
        handler.Received(1).Handle(exception, "OnPageAppearing", source);
    }

    [Test]
    public void InvokeAsync_WhenMethodThrowsBeforeReturningTask_DoesNotEscapeAndReports()
    {
        // Given — a non-async override such as `Task OnXAsync() => throw ...`
        var handler = Substitute.For<ILifecycleExceptionHandler>();
        var exception = new InvalidOperationException("boom");

        // When / Then
        Assert.DoesNotThrow(() => handler.InvokeAsync(() => throw exception, "OnPageAppearingAsync", null));
        handler.Received(1).Handle(exception, "OnPageAppearingAsync", null);
    }

    [Test]
    public async Task InvokeAsync_WhenTaskFaults_Reports()
    {
        // Given
        var handler = Substitute.For<ILifecycleExceptionHandler>();
        var exception = new InvalidOperationException("boom");
        var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.When(h => h.Handle(exception, "OnPageAppearingAsync", null)).Do(_ => reported.TrySetResult());

        // When
        handler.InvokeAsync(async () => { await Task.Yield(); throw exception; }, "OnPageAppearingAsync", null);

        // Then
        var completed = await Task.WhenAny(reported.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.That(completed, Is.SameAs(reported.Task), "the faulted task was never reported");
    }
}
