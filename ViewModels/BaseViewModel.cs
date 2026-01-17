using System.ComponentModel;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
	private PropertyChangedEventHandler? _handler;
	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add => _handler += value;
		remove => _handler -= value;
	}
	
	protected void OnPropertyChanged(PropertyChangedEventArgs args)
	{
		_handler?.Invoke(this, args);
	}

	protected string TypeName => GetType().Name;

	internal virtual string PageName => TypeName.Replace("ViewModel", "Page");

	protected string ViewModelName => TypeName;

	protected string NormalizedName => TypeName.Replace("ViewModel", string.Empty);
}
