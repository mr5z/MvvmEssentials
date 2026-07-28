using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;

namespace Nkraft.MvvmEssentials.Services.Helpers;

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
        var serviceProvider = Application.Current?.Handler?.MauiContext?.Services;
        var logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<TTargetType>();
        var dispatcher = serviceProvider?.GetService<IDispatcher>();

        if (logger is not null && dispatcher is not null)
        {
            Handle(ex, logger, dispatcher, methodName);
        }
        else
        {
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
    }
}