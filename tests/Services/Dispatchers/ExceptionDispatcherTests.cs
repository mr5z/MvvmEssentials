using Nkraft.MvvmEssentials.Services.Dispatchers;
using NSubstitute;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services.Dispatchers;

[TestFixture]
public class ExceptionDispatcherGenericHandleTests
{
    [TearDown]
    public void TearDown()
    {
        Application.Current = null;
    }

    private static void SetApplicationCurrentWithServices(IServiceProvider services)
    {
        var mauiContext = Substitute.For<IMauiContext>();
        mauiContext.Services.Returns(services);

        var handler = Substitute.For<IElementHandler>();
        handler.MauiContext.Returns(mauiContext);

        Application.SetCurrentApplication(new Application { Handler = handler });
    }

    private static ILifecycleExceptionHandler SetApplicationCurrentWithHandler()
    {
        var handler = Substitute.For<ILifecycleExceptionHandler>();
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        SetApplicationCurrentWithServices(services.BuildServiceProvider());
        return handler;
    }

    // -----------------------------------------------------------------------
    // Handle<T>
    // -----------------------------------------------------------------------

    [Test]
    public void HandleGeneric_WhenHandlerResolves_DelegatesToIt()
    {
        // Given
        var handler = SetApplicationCurrentWithHandler();
        var exception = new InvalidOperationException("boom");
        var source = new object();

        // When
        ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod", source);

        // Then
        handler.Received(1).Handle(exception, "MyMethod", source);
    }

    [Test]
    public void HandleGeneric_WhenHandlerResolves_DoesNotThrow()
    {
        // Given
        SetApplicationCurrentWithHandler();

        // When / Then
        Assert.DoesNotThrow(() =>
            ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(new InvalidOperationException("boom"), "MyMethod"));
    }

    [Test]
    public void HandleGeneric_WhenHandlerIsNotRegistered_RethrowsOriginal()
    {
        // Given — services exist but the handler isn't registered, so the configured
        // behavior is unknown and the default (Crash) is applied directly
        SetApplicationCurrentWithServices(new ServiceCollection().BuildServiceProvider());
        var exception = new InvalidOperationException("boom");

        // When / Then — regardless of the library's own build configuration
        var thrown = Assert.Throws<InvalidOperationException>(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
        Assert.That(thrown, Is.SameAs(exception));
    }

    [Test]
    public void HandleGeneric_WhenNoApplication_RethrowsOriginal()
    {
        // Given
        Application.Current = null;
        var exception = new InvalidOperationException("boom");

        // When / Then
        var thrown = Assert.Throws<InvalidOperationException>(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
        Assert.That(thrown, Is.SameAs(exception));
    }

    // -----------------------------------------------------------------------
    // Invoke<T> / InvokeAsync<T>
    // -----------------------------------------------------------------------

    [Test]
    public void InvokeGeneric_WhenActionThrows_DoesNotEscapeAndReportsToHandler()
    {
        // Given
        var handler = SetApplicationCurrentWithHandler();
        var exception = new InvalidOperationException("boom");
        var source = new object();

        // When / Then
        Assert.DoesNotThrow(() => ExceptionDispatcher.Invoke<ExceptionDispatcherGenericHandleTests>(
            () => throw exception, "OnSomething", source));
        handler.Received(1).Handle(exception, "OnSomething", source);
    }

    [Test]
    public void InvokeGeneric_WhenActionSucceeds_DoesNotReport()
    {
        // Given
        var handler = SetApplicationCurrentWithHandler();
        var ran = false;

        // When
        ExceptionDispatcher.Invoke<ExceptionDispatcherGenericHandleTests>(() => ran = true, "OnSomething", null);

        // Then
        Assert.That(ran, Is.True);
        handler.DidNotReceiveWithAnyArgs().Handle(default!, default!, default);
    }

    [Test]
    public void InvokeAsyncGeneric_WhenMethodThrowsBeforeReturningTask_DoesNotEscapeAndReportsToHandler()
    {
        // Given — a non-async override that throws synchronously
        var handler = SetApplicationCurrentWithHandler();
        var exception = new InvalidOperationException("boom");

        // When / Then
        Assert.DoesNotThrow(() => ExceptionDispatcher.InvokeAsync<ExceptionDispatcherGenericHandleTests>(
            () => throw exception, "OnSomethingAsync", null));
        handler.Received(1).Handle(exception, "OnSomethingAsync", null);
    }

    [Test]
    public async Task InvokeAsyncGeneric_WhenTaskFaults_ReportsToHandler()
    {
        // Given
        var handler = SetApplicationCurrentWithHandler();
        var exception = new InvalidOperationException("boom");
        var reported = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.When(h => h.Handle(exception, "OnSomethingAsync", null)).Do(_ => reported.TrySetResult());

        // When
        ExceptionDispatcher.InvokeAsync<ExceptionDispatcherGenericHandleTests>(
            async () => { await Task.Yield(); throw exception; }, "OnSomethingAsync", null);

        // Then
        var completed = await Task.WhenAny(reported.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.That(completed, Is.SameAs(reported.Task), "the faulted task was never reported");
    }
}
