using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;

namespace Nkraft.MvvmEssentials.Services.Dispatchers;

internal static class ExceptionDispatcher
{
    public static void Handle(
        Exception exception, ILogger logger, IDispatcher dispatcher, string methodName)
    {
        logger.LogError(exception,
            "An error occurred while invoking '{MethodName}'.", methodName);

#if DEBUG
        var captured = ExceptionDispatchInfo.Capture(exception);
        dispatcher.Dispatch(() => captured.Throw());
#endif
    }
    
    public static void Handle<TTargetType>(Exception ex, string methodName)
    {
        if (TryResolveDependencies<TTargetType>(out var logger, out var dispatcher))
        {
            Handle(ex, logger, dispatcher, methodName);
            return;
        }

        // Dependencies couldn't be resolved (e.g. called before MauiContext is
        // ready, or services missing). Don't swallow - surface as best we can so
        // the exception isn't lost the way this handler exists to prevent.
        Debug.WriteLine(
            $"[{typeof(TTargetType).Name}] {methodName} failed and the " +
            $"lifecycle exception handler was unavailable: {ex}");

#if DEBUG
        ExceptionDispatchInfo.Capture(ex).Throw();
#endif
    }
    
    private static bool TryResolveDependencies<TTargetType>(
        [NotNullWhen(true)] out ILogger? logger, 
        [NotNullWhen(true)] out IDispatcher? dispatcher)
    {
        var serviceProvider = Application.Current?.Handler?.MauiContext?.Services;
        logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<TTargetType>();
        dispatcher = serviceProvider?.GetService<IDispatcher>();
        return logger is not null && dispatcher is not null;
    }
}