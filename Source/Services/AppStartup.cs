using Microsoft.Extensions.Logging;

namespace Nkraft.MvvmEssentials.Services;

/// <summary>
/// Defines the application startup contract. Implement this anywhere in your project
/// to control initial navigation. If not implemented, the page marked with
/// <c>isInitial: true</c> in <c>AddPageRegistry</c> will be used automatically.
/// </summary>
public interface IAppStartup
{
    Task OnInitializedAsync();
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
        try
        {
            _startup.OnInitializedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during app startup.");
            _applicationContext.Quit();
        }
    }

}