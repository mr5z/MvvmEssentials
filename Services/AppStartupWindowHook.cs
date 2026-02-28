using Microsoft.Extensions.Logging;

namespace Nkraft.MvvmEssentials.Services;

public sealed class AppStartupWindowHook(
    IAppStartup startup,
    ILogger<AppStartupWindowHook> logger)
{
    private readonly IAppStartup _startup = startup;
    private readonly ILogger<AppStartupWindowHook> _logger = logger;

    public void Attach()
    {
        try
        {
            _startup.OnInitializedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during app startup.");
            Application.Current?.Quit();
        }
    }

}