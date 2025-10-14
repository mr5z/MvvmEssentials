using Nkraft.MvvmEssentials.Services.Navigation;
using System.ComponentModel;
using System.Reflection;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : 
	INotifyPropertyChanged,
	// TODO these three will not be invoked on how TabViewModel is instantiated
	IParameterSetAware,
	IRootPageAware,
	IRootPageAwareAsync
{

	public void OnPropertyChanged(PropertyChangedEventArgs args)
	{
		PropertyChanged?.Invoke(this, args);
	}

	public virtual void OnParametersSet(INavigationParameters parameters) { }

	internal void SetNavigationParameter(string key, object? value)
	{
		var property = GetType().GetProperty(key, BindingFlags.Public | BindingFlags.Instance);
		if (property is not null && property.CanWrite)
		{
			if (value is null)
			{
				property.SetValue(this, value);
			}
			else if (AreTypesEqual(property.PropertyType, value.GetType()))
			{
				property.SetValue(this, value);
			}
		}
	}

	public virtual void OnNavigatedToRoot(INavigationParameters parameters) { }

	public virtual Task OnNavigatedToRootAsync(INavigationParameters parameters) => Task.CompletedTask;

	public event PropertyChangedEventHandler? PropertyChanged;

	protected string TypeName => GetType().Name;

	protected virtual string PageName => TypeName.Replace("ViewModel", "Page");

	protected string ViewModelName => TypeName;

	protected string NormalizedName => TypeName.Replace("ViewModel", string.Empty);

	private static bool AreTypesEqual(Type typeA, Type typeB)
	{
		Type underlyingA = Nullable.GetUnderlyingType(typeA) ?? typeA;
		Type underlyingB = Nullable.GetUnderlyingType(typeB) ?? typeB;
		return underlyingA == underlyingB;
	}
}
