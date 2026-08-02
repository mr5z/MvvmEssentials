using System.ComponentModel;
using Nkraft.MvvmEssentials.Helpers;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : INotifyPropertyChanged
{
	private PropertyChangedEventHandler? _handler;
	event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
	{
		add => _handler += value;
		remove => _handler -= value;
	}
	
	protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
	{
		_handler?.Invoke(this, args);
	}

	protected string ViewModelName => GetType().Name;

	internal virtual string PageName => PageHelper.ToPageName(GetType(), PagePattern.Page);
}
