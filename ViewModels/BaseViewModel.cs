using System.ComponentModel;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
	public void OnPropertyChanged(PropertyChangedEventArgs args)
	{
		PropertyChanged?.Invoke(this, args);
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected string TypeName => GetType().Name;

	internal virtual string PageName => TypeName.Replace("ViewModel", "Page");

	protected string ViewModelName => TypeName;

	protected string NormalizedName => TypeName.Replace("ViewModel", string.Empty);
}
