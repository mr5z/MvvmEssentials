using Microsoft.Extensions.Logging;

namespace Nkraft.MvvmEssentials.Services;

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