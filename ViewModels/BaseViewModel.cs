using Nkraft.MvvmEssentials.Services.Navigation;
using System.ComponentModel;
using System.Reflection;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : INotifyPropertyChanged, IParameterSetAware
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
	{
		PropertyChanged?.Invoke(this, args);
	}

	public void OnParametersSet(INavigationParameters parameters) { }

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

	private static bool AreTypesEqual(Type typeA, Type typeB)
	{
		Type underlyingA = Nullable.GetUnderlyingType(typeA) ?? typeA;
		Type underlyingB = Nullable.GetUnderlyingType(typeB) ?? typeB;
		return underlyingA == underlyingB;
	}
}
