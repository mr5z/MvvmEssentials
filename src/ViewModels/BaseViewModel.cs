using System.ComponentModel;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;
	
	protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
	{
		PropertyChanged?.Invoke(this, args);
	}

	protected string ViewModelName => GetType().Name;
}
