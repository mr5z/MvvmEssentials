using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nkraft.CrossUtility.Extensions;

namespace Nkraft.MvvmEssentials.Services.Dispatchers;

/// <summary>
/// Single place that decides what happens when a ViewModel lifecycle method throws:
/// log, notify <see cref="MvvmEssentialsOptions.OnLifecycleException"/>, then apply
/// <see cref="MvvmEssentialsOptions.LifecycleExceptionBehavior"/>.
/// </summary>
internal interface ILifecycleExceptionHandler
{
	void Handle(Exception exception, string methodName, object? source = null);
}

internal sealed class LifecycleExceptionHandler(
	ILogger<LifecycleExceptionHandler> logger,
	IDispatcher dispatcher,
	IOptions<MvvmEssentialsOptions> options) : ILifecycleExceptionHandler
{
	private readonly ILogger<LifecycleExceptionHandler> _logger = logger;
	private readonly IDispatcher _dispatcher = dispatcher;
	private readonly MvvmEssentialsOptions _options = options.Value;

	public void Handle(Exception exception, string methodName, object? source = null)
	{
		_logger.LogError(exception,
			"An error occurred while invoking '{MethodName}' on '{SourceType}'.",
			methodName, source?.GetType().Name ?? "unknown");

		var context = new LifecycleExceptionContext(exception, methodName, source);
		NotifyCallback(context);

		if (_options.LifecycleExceptionBehavior != LifecycleExceptionBehavior.Crash || context.Handled)
			return;

		// Never rethrow synchronously: the caller is usually inside MAUI's own navigation code,
		// and unwinding it leaves the navigation stack half-updated. Posting to the dispatcher lets
		// the current transition finish, then surfaces the original exception as unhandled.
		var captured = ExceptionDispatchInfo.Capture(exception);
		if (_dispatcher.Dispatch(() => captured.Throw()) == false)
		{
			_logger.LogWarning(
				"Could not dispatch the crash for '{MethodName}'; the exception was logged only.", methodName);
		}
	}

	private void NotifyCallback(LifecycleExceptionContext context)
	{
		if (_options.OnLifecycleException is not { } callback)
			return;

		try
		{
			callback(context);
		}
		catch (Exception callbackException)
		{
			_logger.LogError(callbackException,
				"'{Callback}' threw while handling an exception from '{MethodName}'.",
				nameof(MvvmEssentialsOptions.OnLifecycleException), context.MethodName);
		}
	}
}

internal static class LifecycleExceptionHandlerExtension
{
	/// <summary>Runs a synchronous lifecycle method; any exception goes to the handler instead of the caller.</summary>
	public static void Invoke(this ILifecycleExceptionHandler handler, Action action, string methodName, object? source)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			handler.Handle(ex, methodName, source);
		}
	}

	/// <summary>
	/// Starts an async lifecycle method without awaiting it. Covers both a faulted task and a
	/// non-async override that throws before returning a task.
	/// </summary>
	public static void InvokeAsync(this ILifecycleExceptionHandler handler, Func<Task> action, string methodName, object? source)
	{
		Task task;
		try
		{
			task = action();
		}
		catch (Exception ex)
		{
			handler.Handle(ex, methodName, source);
			return;
		}

		task.FireAndForget(ex => handler.Handle(ex, methodName, source));
	}
}
