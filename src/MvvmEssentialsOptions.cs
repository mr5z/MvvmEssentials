// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials;

/// <summary>
/// Library-wide options, configured through the <c>ConfigureMvvmEssentials(registry => ..., options => ...)</c> overload.
/// </summary>
public sealed class MvvmEssentialsOptions
{
	/// <summary>
	/// What happens after an exception escapes a ViewModel lifecycle method
	/// (<c>OnInitialized</c>, <c>OnPageAppearing</c>, <c>OnNavigatedTo</c>, <c>OnTabSelected</c>, ... and their async versions).
	/// Defaults to <see cref="LifecycleExceptionBehavior.Crash"/>.
	/// </summary>
	public LifecycleExceptionBehavior LifecycleExceptionBehavior { get; set; } = LifecycleExceptionBehavior.Crash;

	/// <summary>
	/// Called for every exception that escapes a ViewModel lifecycle method, before
	/// <see cref="LifecycleExceptionBehavior"/> is applied. Use it to report errors to one central place
	/// (crash reporting, analytics, a global error UI).
	/// Set <see cref="LifecycleExceptionContext.Handled"/> to <see langword="true"/> to stop the app
	/// from crashing for that particular exception.
	/// </summary>
	/// <remarks>
	/// Exceptions thrown by this callback are logged and otherwise ignored.
	/// Do not assume it runs on the UI thread: failures from async lifecycle methods are reported
	/// on whichever thread the task completed on.
	/// </remarks>
	public Action<LifecycleExceptionContext>? OnLifecycleException { get; set; }
}

/// <summary>
/// What the library does after an exception escapes a ViewModel lifecycle method.
/// In both cases the exception is logged and <see cref="MvvmEssentialsOptions.OnLifecycleException"/> is called first,
/// and the page lifecycle (navigation, scope disposal) always completes.
/// </summary>
public enum LifecycleExceptionBehavior
{
	/// <summary>
	/// Rethrows the original exception (with its original stack trace) on the UI dispatcher, once the
	/// current navigation has finished. It surfaces as an unhandled exception and normally terminates the app.
	/// </summary>
	Crash,

	/// <summary>
	/// Logs the exception and keeps the app running.
	/// </summary>
	LogOnly,
}

/// <summary>
/// Describes an exception that escaped a ViewModel lifecycle method.
/// </summary>
public sealed class LifecycleExceptionContext
{
	public LifecycleExceptionContext(Exception exception, string methodName, object? source = null)
	{
		Exception = exception;
		MethodName = methodName;
		Source = source;
	}

	/// <summary>The exception that was thrown.</summary>
	public Exception Exception { get; }

	/// <summary>
	/// The lifecycle method the library was invoking, e.g. <c>OnPageAppearing</c>.
	/// <c>OnInitialized</c>/<c>OnInitializedAsync</c> run as part of <c>OnPageAppearing</c>/<c>OnPageAppearingAsync</c>,
	/// so they are reported under those names; the stack trace shows the exact method.
	/// </summary>
	public string MethodName { get; }

	/// <summary>The ViewModel (or tab/flyout component) whose method threw, when known.</summary>
	public object? Source { get; }

	/// <summary>
	/// Set to <see langword="true"/> to prevent <see cref="LifecycleExceptionBehavior.Crash"/> for this exception.
	/// Has no effect with <see cref="LifecycleExceptionBehavior.LogOnly"/>.
	/// </summary>
	public bool Handled { get; set; }
}
