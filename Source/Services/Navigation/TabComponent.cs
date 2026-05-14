namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface ITabComponent
{
    void OnTabSelected();

    void OnTabUnselected();
    
    Task OnTabSelectedAsync();
    
    Task OnTabUnselectedAsync();
}