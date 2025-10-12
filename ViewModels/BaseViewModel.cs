using Nkraft.MvvmEssentials.Services.Navigation;
using System.ComponentModel;
using System.Reflection;

namespace Nkraft.MvvmEssentials.ViewModels;

public class BaseViewModel : 
	INotifyPropertyChanged, 
	IParameterSetAware,
	INavigatedAware,
	INavigatedAwareAsync
{
	private bool _isInitialized = false;
	private bool _isInitializedAsync = false;

	void INavigatedAware.OnNavigatedTo()
	{
		if (_isInitialized == false)
		{
			_isInitialized = true;
			OnInitialized();
		}
	}

	async Task INavigatedAwareAsync.OnNavigatedToAsync()
	{
		if (_isInitializedAsync == false)
		{
			_isInitializedAsync = true;
			await OnInitializedAsync();
		}
	}

	internal void OnPropertyChanged(PropertyChangedEventArgs args)
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

	protected virtual void OnInitialized() { }

	protected virtual Task OnInitializedAsync() => Task.CompletedTask;

	// TODO abstract this away
	void INavigatedAware.OnNavigatedFrom() { }

	// TODO abstract this away
	Task INavigatedAwareAsync.OnNavigatedFromAsync() => Task.CompletedTask;

	public event PropertyChangedEventHandler? PropertyChanged;

	private static bool AreTypesEqual(Type typeA, Type typeB)
	{
		Type underlyingA = Nullable.GetUnderlyingType(typeA) ?? typeA;
		Type underlyingB = Nullable.GetUnderlyingType(typeB) ?? typeB;
		return underlyingA == underlyingB;
	}
}
