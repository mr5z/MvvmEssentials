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