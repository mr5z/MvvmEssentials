namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IHostComponent
{
    void OnTabSelected();

    void OnTabUnselected();
    
    Task OnTabSelectedAsync();
    
    Task OnTabUnselectedAsync();
}