using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    [Test]
    public void HandleGeneric_WhenLoggerAndDispatcherBothResolve_DoesNotThrow()
    {
        // Given — both dependencies available via the (substituted) MauiContext
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(Substitute.For<IDispatcher>());
        using var serviceProvider = services.BuildServiceProvider();
        SetApplicationCurrentWithServices(serviceProvider);

        var exception = new InvalidOperationException("boom");

        // Then — delegates to the already-tested 4-arg Handle(), which only *records* a
        // dispatch on the substitute dispatcher rather than executing the rethrow action
        Assert.DoesNotThrow(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
    }

    [Test]
    public void HandleGeneric_WhenLoggerAndDispatcherBothResolve_LogsThroughTheResolvedLogger()
    {
        // Given
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(Substitute.For<IDispatcher>());
        using var serviceProvider = services.BuildServiceProvider();
        SetApplicationCurrentWithServices(serviceProvider);

        var exception = new InvalidOperationException("boom");

        // When
        ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod");

        // Then
        Assert.That(logger.ReceivedCalls(), Is.Not.Empty);
    }

    [Test]
    public void HandleGeneric_WhenOnlyLoggerResolves_FallsBackAndRethrows()
    {
        // Given — IDispatcher deliberately not registered, so the && condition's right
        // side is independently false (distinct from the all-null case elsewhere, where
        // the left side already short-circuits)
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        using var serviceProvider = services.BuildServiceProvider();
        SetApplicationCurrentWithServices(serviceProvider);

        var exception = new InvalidOperationException("boom");

#if DEBUG
        // When / Then
        var thrown = Assert.Throws<InvalidOperationException>(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
        Assert.That(thrown, Is.SameAs(exception));
#else
        Assert.DoesNotThrow(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
#endif
    }

    [Test]
    public void HandleGeneric_WhenOnlyDispatcherResolves_FallsBackAndRethrows()
    {
        // Given — ILoggerFactory deliberately not registered
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDispatcher>());
        using var serviceProvider = services.BuildServiceProvider();
        SetApplicationCurrentWithServices(serviceProvider);

        var exception = new InvalidOperationException("boom");

#if DEBUG
        var thrown = Assert.Throws<InvalidOperationException>(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
        Assert.That(thrown, Is.SameAs(exception));
#else
        Assert.DoesNotThrow(
            () => ExceptionDispatcher.Handle<ExceptionDispatcherGenericHandleTests>(exception, "MyMethod"));
#endif
    }
}