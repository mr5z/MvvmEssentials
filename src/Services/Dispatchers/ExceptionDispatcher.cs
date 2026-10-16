using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Nkraft.CrossUtility.Extensions;

namespace Nkraft.MvvmEssentials.Services.Dispatchers;

/// <summary>
/// Static access to <see cref="ILifecycleExceptionHandler"/> for code that is not created by DI,
/// such as XAML-instantiated behaviors. The handler is resolved only when something fails.
/// </summary>
internal static class ExceptionDispatcher
{
	/// <summary>Runs a synchronous lifecycle method; any exception goes to the handler instead of the caller.</summary>
	public static void Invoke<TTargetType>(Action action, string methodName, object? source)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Handle<TTargetType>(ex, methodName, source);
		}
	}

	/// <summary>Starts an async lifecycle method without awaiting it; a sync throw or a faulted task goes to the handler.</summary>
	public static void InvokeAsync<TTargetType>(Func<Task> action, string methodName, object? source)
	{
		Task task;
		try
		{
			task = action();
		}
		catch (Exception ex)
		{
			Handle<TTargetType>(ex, methodName, source);
			return;
		}

		task.FireAndForget(ex => Handle<TTargetType>(ex, methodName, source));
	}

	public static void Handle<TTargetType>(Exception ex, string methodName, object? source = null)
	{
		if (TryResolveHandler(out var handler))
		{
			handler.Handle(ex, methodName, source);
			return;
		}

		// The handler couldn't be resolved (e.g. called before MauiContext is ready, or in a test
		// without an Application), so the configured options are unknown. Don't swallow: apply the
		// default behavior (Crash) directly so the exception isn't lost.
		Debug.WriteLine(
			$"[{typeof(TTargetType).Name}] {methodName} failed and the " +
			$"lifecycle exception handler was unavailable: {ex}");

		ExceptionDispatchInfo.Capture(ex).Throw();
	}

	private static bool TryResolveHandler([NotNullWhen(true)] out ILifecycleExceptionHandler? handler)
	{
		var services = Application.Current?.Handler?.MauiContext?.Services;
		handler = services?.GetService<ILifecycleExceptionHandler>();
		return handler is not null;
	}
}
