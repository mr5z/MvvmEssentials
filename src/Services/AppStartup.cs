using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services;

/// <summary>
/// Defines the application startup contract. Implement this anywhere in your project
/// to control initial navigation. If not implemented, the page marked with
/// <c>isInitial: true</c> in <c>MapPage</c> will be used automatically.
/// </summary>
public interface IAppStartup
{
    /// <summary>
    /// Navigates to the first page and returns the result of that navigation.
    /// A failed result is rethrown with its original exception, since the app has no page to show.
    /// </summary>
    Task<IResult> OnInitializedAsync();
}

public sealed class AppStartupWindowHook(
    ILogger<AppStartupWindowHook> logger,
    IAppStartup startup,
    IApplicationContext applicationContext)
{
    private readonly ILogger<AppStartupWindowHook> _logger = logger;
    private readonly IAppStartup _startup = startup;
    private readonly IApplicationContext _applicationContext = applicationContext;

    public void Attach()
    {
        IResult result;
        try
        {
            result = _startup.OnInitializedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during app startup.");
            throw;
        }

        // The failure was already logged by the navigation service. Rethrow the original
        // exception: without a root page MAUI would otherwise fail with a generic "set MainPage" error.
        result.ThrowIfFailure();

        if (_applicationContext.MainPage is null)
        {
            const string message =
                $"{nameof(IAppStartup)}.{nameof(IAppStartup.OnInitializedAsync)} returned success but no root page was set. " +
                "Navigate to a page and return the result of that navigation.";
            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
}
