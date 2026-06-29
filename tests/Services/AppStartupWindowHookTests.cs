using Microsoft.Extensions.Logging.Abstractions;
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
    private AppStartupWindowHook _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _startup = Substitute.For<IAppStartup>();
        _applicationContext = Substitute.For<IApplicationContext>();

        // NullLogger avoids Castle DynamicProxy issues with ILogger<T> over internal types
        _sut = new AppStartupWindowHook(
            NullLogger<AppStartupWindowHook>.Instance,
            _startup,
            _applicationContext);
    }

    // -----------------------------------------------------------------------
    // Happy path
    // -----------------------------------------------------------------------

    [Test]
    public void Attach_WhenStartupSucceeds_CallsOnInitializedAsync()
    {
        // Given
        _startup.OnInitializedAsync().Returns(Task.CompletedTask);

        // When
        _sut.Attach();

        // Then
        _startup.Received(1).OnInitializedAsync();
    }

    [Test]
    public void Attach_WhenStartupSucceeds_DoesNotCallQuit()
    {
        // Given
        _startup.OnInitializedAsync().Returns(Task.CompletedTask);

        // When
        _sut.Attach();

        // Then
        _applicationContext.DidNotReceive().Quit();
    }

    // -----------------------------------------------------------------------
    // Error path
    // -----------------------------------------------------------------------

    [Test]
    public void Attach_WhenStartupThrows_CallsQuit()
    {
        // Given
        _startup.OnInitializedAsync()
            .Throws(new InvalidOperationException("startup boom"));

        // When
        _sut.Attach();

        // Then
        _applicationContext.Received(1).Quit();
    }

    [Test]
    public void Attach_WhenStartupThrows_DoesNotPropagateException()
    {
        // Given
        _startup.OnInitializedAsync()
            .Throws(new InvalidOperationException("startup boom"));

        // Then
        Assert.DoesNotThrow(() => _sut.Attach());
    }
}
