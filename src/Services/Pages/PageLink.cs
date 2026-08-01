using System.Globalization;
using System.Net;
using Nkraft.CrossUtility.Helpers;

namespace Nkraft.MvvmEssentials.Services.Pages;

internal sealed class PageWithQuery(string? pageName, object? parameters)
{
	public string? PageName { get; } = pageName;

	public object? Parameters { get; } = parameters;

	public string? GetResolvedName()
	{
		if (Parameters is null)
			return PageName;

		var queryString = Parameters is INavigationParameters navigationParameters
			? ToQueryString(navigationParameters)
			: QueryStringHelper.ToQueryString(Parameters);
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

	IPageLink AppendSegment(string pageName, object? parameters = null);
}

internal class PageLink(INavigationService navigationService) : IPageLink
{
	private readonly List<PageWithQuery> _pages = [];

	public PageLink(INavigationService navigationService, string? rootPage) : this(navigationService)
	{
		AppendSegmentImplied(rootPage, parameters: null);
	}

	IPageLink IPageLink.AppendSegment(string? pageName, object? parameters)
	{
		AppendSegmentImplied(pageName, parameters);
		return this;
	}

	private void AppendSegmentImplied(string? pageName, object? parameters)
	{
		if (string.IsNullOrEmpty(pageName))
			// 	const string message = "Page name cannot be null or empty";
			// 	_logger.LogWarning(error);
			return;
		
		var invalidNames = GetUnserializableNames(parameters);
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
		
		_pages.Add(new PageWithQuery(pageName, parameters));
	}

	public INavigationService NavigationService { get; private set; } = navigationService;

	string IPageLink.FullPath => string.Join('/', _pages.Select(p => p.GetResolvedName()));
	
	private static string[] GetUnserializableNames(object? parameters)
	{
		return parameters switch
		{
			null => [],
			INavigationParameters navigationParameters =>
			[
				.. navigationParameters
					.Where(pair => pair.Value is not null && IsPathSafe(pair.Value.GetType()) == false)
					.Select(pair => pair.Key)
			],
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
