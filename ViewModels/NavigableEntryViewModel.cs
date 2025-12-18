using Nkraft.MvvmEssentials.Services.Navigation;
using System.Reflection;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class NavigableEntryViewModel : BaseViewModel,
	IParameterSetAware,
	IRootPageAware,
	IRootPageAwareAsync
{
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

	public virtual void OnParametersSet(INavigationParameters parameters) { }

	public virtual void OnNavigatedToRoot(INavigationParameters parameters) { }

	public virtual Task OnNavigatedToRootAsync(INavigationParameters parameters) => Task.CompletedTask;

	private static bool AreTypesEqual(Type typeA, Type typeB)
	{
		Type underlyingA = Nullable.GetUnderlyingType(typeA) ?? typeA;
		Type underlyingB = Nullable.GetUnderlyingType(typeB) ?? typeB;
		return underlyingA == underlyingB;
	}
}
