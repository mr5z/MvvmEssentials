// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials.Services;

public interface ITabComponent
{
    void OnTabSelected();

    void OnTabUnselected();
    
    Task OnTabSelectedAsync();
    
    Task OnTabUnselectedAsync();
}