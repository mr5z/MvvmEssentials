using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using Nkraft.CrossUtility.Helpers;

namespace Nkraft.MvvmEssentials.Services.Pages;

// The query string is built lazily, when FullPath is read, as before. Holding a delegate rather than the raw
// parameters keeps the reflection-based serializer reachable only from the [RequiresUnreferencedCode] path.
internal sealed class PageWithQuery(string? pageName, Func<string>? getQueryString)
{
	public string? PageName { get; } = pageName;

	public static PageWithQuery FromNavigationParameters(string? pageName, INavigationParameters? parameters)
	{
		return new PageWithQuery(pageName, parameters is null ? null : () => ToQueryString(parameters));
	}

	[RequiresUnreferencedCode(PageLink.ObjectParametersMessage)]
	public static PageWithQuery FromObject(string? pageName, object? parameters)
	{
		return parameters switch
		{
			null => new PageWithQuery(pageName, null),
			INavigationParameters navigationParameters => FromNavigationParameters(pageName, navigationParameters),
			_ => new PageWithQuery(pageName, () => QueryStringHelper.ToQueryString(parameters))
		};
	}

	public string? GetResolvedName()
	{
		if (getQueryString is null)
			return PageName;

		var queryString = getQueryString();
		return string.IsNullOrEmpty(queryString) ? PageName : $"{PageName}?{queryString}";
	}
	
	private static string ToQueryString(INavigationParameters parameters)
	{
		return string.Join("&", parameters
			.Where(e => e.Value is not null)
			.Select(e => $"{e.Key}={WebUtility.UrlEncode(e.Value!.ToString())}"));
	}

	private static string Format(object value)
	{
		return value is IFormattable formattable
			? formattable.ToString(null, CultureInfo.InvariantCulture)
			: value.ToString() ?? string.Empty;
	}
}

public interface IPageLink
{
	string FullPath { get; }

	[RequiresUnreferencedCode(PageLink.ObjectParametersMessage)]
	IPageLink AppendSegment(string pageName, object? parameters = null);
}

internal class PageLink(INavigationService navigationService) : IPageLink
{
	internal const string ObjectParametersMessage =
		"Reads the parameters object's properties by reflection, which trimming can remove. " +
		"Pass a PageDestination from a generated With(...) factory instead.";

	private readonly List<PageWithQuery> _pages = [];

	public PageLink(INavigationService navigationService, string? rootPage) : this(navigationService)
	{
		AppendSegment(rootPage, parameters: null);
	}

	[RequiresUnreferencedCode(ObjectParametersMessage)]
	IPageLink IPageLink.AppendSegment(string? pageName, object? parameters)
	{
		if (string.IsNullOrEmpty(pageName))
			return this;

		ThrowIfUnserializable(pageName, GetUnserializableNames(parameters));
		_pages.Add(PageWithQuery.FromObject(pageName, parameters));
		return this;
	}

	/// <summary>Appends a segment without reflection: its parameters are already key/value pairs.</summary>
	internal IPageLink AppendSegment(string? pageName, INavigationParameters? parameters)
	{
		if (string.IsNullOrEmpty(pageName))
			// 	const string message = "Page name cannot be null or empty";
			// 	_logger.LogWarning(error);
			return this;

		ThrowIfUnserializable(pageName, GetUnserializableNames(parameters));
		_pages.Add(PageWithQuery.FromNavigationParameters(pageName, parameters));
		return this;
	}

	private static void ThrowIfUnserializable(string pageName, string[] invalidNames)
	{
		if (invalidNames.Length > 0)
		{
			// TODO find a way to access the logger without converting this a member function
			// const string message = "Invalid parameter types found '{Properties}'. It must be either string or number only.";
			// _logger.LogWarning(error, string.Join(", ", invalidProps.Select(p => p.Name)));
			throw new InvalidOperationException(
				$"Cannot append segment '{pageName}': parameter(s) '{string.Join(", ", invalidNames)}' " +
				"are not representable in a navigation path. Path segments support string, numeric, " +
				"bool, char, Guid and enum values only. Use NavigateAsync(destination) for richer types.");
		}
	}

	public INavigationService NavigationService { get; private set; } = navigationService;

	string IPageLink.FullPath => string.Join('/', _pages.Select(p => p.GetResolvedName()));
	
	private static string[] GetUnserializableNames(INavigationParameters? parameters)
	{
		return parameters is null
			? []
			:
			[
				.. parameters
					.Where(pair => pair.Value is not null && IsPathSafe(pair.Value.GetType()) == false)
					.Select(pair => pair.Key)
			];
	}

	[RequiresUnreferencedCode(ObjectParametersMessage)]
	private static string[] GetUnserializableNames(object? parameters)
	{
		return parameters switch
		{
			null => [],
			INavigationParameters navigationParameters => GetUnserializableNames(navigationParameters),
			_ =>
			[
				.. parameters.GetType().GetProperties()
					.Where(p => IsPathSafe(p.PropertyType) == false)
					.Select(p => p.Name)
			]
		};
	}

	private static readonly HashSet<Type> PathSafeTypes =
	[
		typeof(string), typeof(bool),   typeof(char),   typeof(Guid),
		typeof(int),    typeof(long),   typeof(short),  typeof(byte),
		typeof(uint),   typeof(ulong),  typeof(ushort), typeof(sbyte),
		typeof(float),  typeof(double), typeof(decimal)
	];

	private static bool IsPathSafe(Type t)
	{
		t = Nullable.GetUnderlyingType(t) ?? t;
		return t.IsEnum || PathSafeTypes.Contains(t);
	}
}
