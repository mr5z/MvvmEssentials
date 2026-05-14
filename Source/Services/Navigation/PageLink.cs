using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Helpers;

namespace Nkraft.MvvmEssentials.Services.Navigation;

internal sealed class PageWithQuery(string? pageName, object? parameters)
{
	public string? PageName { get; } = pageName;

	public object? Parameters { get; } = parameters;

	public string? GetResolvedName()
	{
		if (Parameters is null)
			return PageName;

		var queryString = QueryStringHelper.ToQueryString(Parameters);
		return $"{PageName}?{queryString}";
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
		if (string.IsNullOrEmpty(pageName) == false)
		{
			var invalidProps = parameters?.GetType().GetProperties()
				.Where(p => !IsNumericOrString(p.PropertyType))
				.ToList();

			if (invalidProps?.Count > 0)
			{
				// TODO find a way to access the logger without converting this a member function
				// const string error = "Invalid parameter types found '{Properties}'. It must be either string or number only.";
				// _logger.LogWarning(error, string.Join(", ", invalidProps.Select(p => p.Name)));
			}
			
			_pages.Add(new PageWithQuery(pageName, parameters));
		}
		// else
		// {
		// 	const string error = "Page name cannot be null or empty";
		// 	_logger.LogWarning(error);
		// }
	}

	public INavigationService NavigationService { get; private set; } = navigationService;

	string IPageLink.FullPath => string.Join('/', _pages.Select(p => p.GetResolvedName()));
	
	private static bool IsNumericOrString(Type t)
	{
		t = Nullable.GetUnderlyingType(t) ?? t;
		return t == typeof(string)
		       || t == typeof(int)    || t == typeof(long)
		       || t == typeof(float)  || t == typeof(double)
		       || t == typeof(decimal)|| t == typeof(short)
		       || t == typeof(byte);
	}
}
