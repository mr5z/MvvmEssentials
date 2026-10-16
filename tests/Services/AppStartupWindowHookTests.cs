using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Nkraft.MvvmEssentials.UnitTest.Services;

[TestFixture]
public class AppStartupWindowHookTests
{
    private IAppStartup _startup = null!;
    private IApplicationContext _applicationContext = null!;
    private RecordingLogger<AppStartupWindowHook> _logger = null!;
    private AppStartupWindowHook _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _startup = Substitute.For<IAppStartup>();

        // A root page exists unless a test says otherwise
        _applicationContext = Substitute.For<IApplicationContext>();
        _applicationContext.MainPage.Returns(new Page());

        // Hand-rolled logger instead of a substitute: avoids Castle DynamicProxy
        // issues with ILogger<T>, and lets us assert what was logged.
        _logger = new RecordingLogger<AppStartupWindowHook>();

        _sut = new AppStartupWindowHook(_logger, _startup, _applicationContext);
    }

    private static Task<IResult> Ok() => Task.FromResult<IResult>(Result.Ok());

    // -----------------------------------------------------------------------
    // Happy path
    // -----------------------------------------------------------------------

    [Test]
    public void Attach_WhenStartupSucceeds_CallsOnInitializedAsync()
    {
        // Given
        _startup.OnInitializedAsync().Returns(Ok());

        // When
        _sut.Attach();

        // Then
        _startup.Received(1).OnInitializedAsync();
    }

    [Test]
    public void Attach_WhenStartupSucceeds_DoesNotThrowOrLog()
    {
        // Given
        _startup.OnInitializedAsync().Returns(Ok());

        // Then
        Assert.DoesNotThrow(() => _sut.Attach());
        Assert.That(_logger.Entries, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // Error path: log, then rethrow (fail fast). Two shapes of failure:
    //  - Throws:  the call itself throws before returning a Task
    //  - Faulted: a real async method that fails returns a faulted Task
    // -----------------------------------------------------------------------

    private static IEnumerable<TestCaseData> FailureShapes()
    {
        yield return new TestCaseData(true).SetName("{m}(Throws)");
        yield return new TestCaseData(false).SetName("{m}(Faulted)");
    }

    private InvalidOperationException ArrangeFailure(bool throwsSynchronously)
    {
        var boom = new InvalidOperationException("startup boom");
        if (throwsSynchronously)
            _startup.OnInitializedAsync().Throws(boom);
        else
            _startup.OnInitializedAsync().Returns(Task.FromException<IResult>(boom));
        return boom;
    }

    [TestCaseSource(nameof(FailureShapes))]
    public void Attach_WhenStartupFails_RethrowsTheOriginalException(bool throwsSynchronously)
    {
        // Given
        var boom = ArrangeFailure(throwsSynchronously);

        // When
        var thrown = Assert.Throws<InvalidOperationException>(() => _sut.Attach());

        // Then: same instance, not wrapped (e.g. no AggregateException)
        Assert.That(thrown, Is.SameAs(boom));
    }

    [TestCaseSource(nameof(FailureShapes))]
    public void Attach_WhenStartupFails_LogsOneErrorWithTheException(bool throwsSynchronously)
    {
        // Given
        var boom = ArrangeFailure(throwsSynchronously);

        // When
        Assert.Throws<InvalidOperationException>(() => _sut.Attach());

        // Then
        Assert.That(_logger.Entries, Has.Count.EqualTo(1));
        Assert.That(_logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
        Assert.That(_logger.Entries[0].Exception, Is.SameAs(boom));
    }

    // -----------------------------------------------------------------------
    // Returned result: a failed navigation is rethrown even though the user
    // never called ThrowIfFailure()
    // -----------------------------------------------------------------------

    [Test]
    public void Attach_WhenStartupReturnsFailedResult_RethrowsItsException()
    {
        // Given — e.g. the startup page's ViewModel constructor threw
        var boom = new InvalidOperationException("viewmodel boom");
        _startup.OnInitializedAsync().Returns(Task.FromResult<IResult>(Result.Fail(ErrorCode.Unknown, boom, "navigation failed")));
        _applicationContext.MainPage.Returns((Page?)null);

        // When
        var thrown = Assert.Throws<InvalidOperationException>(() => _sut.Attach());

        // Then — the real cause, not a "no root page" error
        Assert.That(thrown, Is.SameAs(boom));
    }

    [Test]
    public void Attach_WhenStartupReturnsFailedResult_DoesNotLogAgain()
    {
        // Given — the navigation service already logged the failure
        _startup.OnInitializedAsync().Returns(Task.FromResult<IResult>(
            Result.Fail(ErrorCode.Unknown, new InvalidOperationException("boom"), "navigation failed")));

        // When
        Assert.Throws<InvalidOperationException>(() => _sut.Attach());

        // Then
        Assert.That(_logger.Entries, Is.Empty);
    }

    [Test]
    public void Attach_WhenStartupSucceedsButSetsNoRootPage_ThrowsAndLogs()
    {
        // Given — e.g. `return Result.Ok();` without navigating
        _startup.OnInitializedAsync().Returns(Ok());
        _applicationContext.MainPage.Returns((Page?)null);

        // When
        var thrown = Assert.Throws<InvalidOperationException>(() => _sut.Attach());

        // Then
        Assert.That(thrown!.Message, Does.Contain("no root page was set"));
        Assert.That(_logger.Entries, Has.Count.EqualTo(1));
        Assert.That(_logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
    }

    // -----------------------------------------------------------------------
    // Test double
    // -----------------------------------------------------------------------

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception? Exception, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, exception, formatter(state, exception)));
    }
}