using System.Reflection;
using Nkraft.MvvmEssentials.Attributes;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;

namespace Nkraft.MvvmEssentials.ViewModels;

public abstract class NavigableEntryViewModel : BaseViewModel,
	IParametersSet,
	IRootPageNavigated
{
	internal void SetNavigationParameter(string key, object? value)
	{
		const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var property = GetType().GetProperty(key, bindingFlags);
		var hasParameterAttribute = property?.GetCustomAttributes<NavigationParameterAttribute>().Any();
		if (hasParameterAttribute == false || property is null || property.CanWrite == false)
			return;
		
		if (value is null || AreTypesEqual(property.PropertyType, value.GetType()))
		{
			property.SetValue(this, value);
		}
	}
	
	protected virtual void OnParametersSet(INavigationParameters parameters) { }

	protected virtual void OnNavigatedToRoot(INavigationParameters parameters) { }

	protected virtual Task OnNavigatedToRootAsync(INavigationParameters parameters) => Task.CompletedTask;

	void IParametersSet.OnParametersSet(INavigationParameters parameters) => OnParametersSet(parameters);
	
	void IRootPageNavigated.OnNavigatedToRoot(INavigationParameters parameters) => OnNavigatedToRoot(parameters);
	
	Task IRootPageNavigated.OnNavigatedToRootAsync(INavigationParameters parameters) => OnNavigatedToRootAsync(parameters);

	private static bool AreTypesEqual(Type typeA, Type typeB)
	{
		var underlyingA = Nullable.GetUnderlyingType(typeA) ?? typeA;
		var underlyingB = Nullable.GetUnderlyingType(typeB) ?? typeB;
		return underlyingA == underlyingB;
	}
}
