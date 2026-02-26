using Microsoft.Extensions.Logging;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services;

public sealed class AppStartupWindowHook(
    IAppStartup startup,
    IWindowEventHandler windowEvent,
    ILogger<AppStartupWindowHook> logger)
{
    private readonly IAppStartup _startup = startup;
    private readonly IWindowEventHandler _windowEvent = windowEvent;
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
        
        // window.Created     += (s, e) => _windowEvent.OnCreated();
        // window.Destroying  += (s, e) => _windowEvent.OnDestroying();
        // window.Activated   += (s, e) => _windowEvent.OnActivated();
        // window.Deactivated += (s, e) => _windowEvent.OnDeactivated();
        // window.Resumed     += (s, e) => _windowEvent.OnResumed();
        // window.Stopped     += (s, e) => _windowEvent.OnStopped();
    }

}