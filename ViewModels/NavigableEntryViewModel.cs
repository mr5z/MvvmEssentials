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
		if (property is null || property.CanWrite == false)
		{
			return;
		}
		if (value is null || AreTypesEqual(property.PropertyType, value.GetType()))
		{
			property.SetValue(this, value);
		}
	}
	
	protected virtual void OnParametersSet(INavigationParameters parameters) { }

	protected virtual void OnNavigatedToRoot(INavigationParameters parameters) { }

	protected virtual Task OnNavigatedToRootAsync(INavigationParameters parameters) => Task.CompletedTask;

	void IParameterSetAware.OnParametersSet(INavigationParameters parameters) => OnParametersSet(parameters);
	
	void IRootPageAware.OnNavigatedToRoot(INavigationParameters parameters) => OnNavigatedToRoot(parameters);
	
	Task IRootPageAwareAsync.OnNavigatedToRootAsync(INavigationParameters parameters) => OnNavigatedToRootAsync(parameters);

	private static bool AreTypesEqual(Type typeA, Type typeB)
	{
		var underlyingA = Nullable.GetUnderlyingType(typeA) ?? typeA;
		var underlyingB = Nullable.GetUnderlyingType(typeB) ?? typeB;
		return underlyingA == underlyingB;
	}
}
