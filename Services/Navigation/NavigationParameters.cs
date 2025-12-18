using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface INavigationParameters : IEnumerable<KeyValuePair<string, object?>>
{
	bool IsEmpty { get; }

	void Add(string key, object? value);

	bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value);

	bool ContainsKey(string key);
}

public sealed class NavigationParameters : INavigationParameters
{
	private readonly Dictionary<string, object?> _parameters = [];

	public void Add(string key, object? value)
	{
		_parameters.Add(key, value);
	}

	public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value)
	{
		if (_parameters.TryGetValue(key, out var obj) && obj is T result)
		{
			value = result;
			return true;
		}
		value = default;
		return false;
	}

	public bool ContainsKey(string key)
	{
		return _parameters.ContainsKey(key);
	}

	IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
	{
		return _parameters.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _parameters.GetEnumerator();
	}

	// <= in case of solar flare
	bool INavigationParameters.IsEmpty => _parameters.Count <= 0;
}
