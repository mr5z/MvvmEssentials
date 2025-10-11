using Nkraft.CrossUtility.Helpers;

namespace Nkraft.MvvmEssentials.Services.Navigation;

internal class PageWithQuery(string? pageName)
{
	public PageWithQuery(string? pageName, Type? pageType) : this(pageName)
	{
		PageType = pageType;
	}

	public string? PageName { get; } = pageName;

	public Type? PageType { get; }

	public object? Parameters { get; set; }

	public string? GetResolvedName()
	{
		if (Parameters is null)
		{
			return PageName;
		}

		var queryString = QueryStringHelper.ToQueryString(Parameters);
		return $"{PageName}?{queryString}";
	}
}

public interface IPageLink
{
	string FullPath { get; }

	IEnumerable<Type?> PageTypes { get; }

	IPageLink AppendSegment(string pageName, object? parameters = null);

	IPageLink AppendSegment(string pageName, Type pageType, object? parameters = null);
}

internal class PageLink(INavigationService navigationService) : IPageLink
{
	private readonly List<PageWithQuery> _pages = [];

	public PageLink(INavigationService navigationService, string? start) : this(navigationService)
	{
		if (string.IsNullOrEmpty(start))
		{ 
			_pages.Add(new PageWithQuery(start));
		}
	}

	IPageLink IPageLink.AppendSegment(string pageName, object? parameters)
	{
		return AppendSegmentImplied(pageName, null, parameters);
	}

	IPageLink IPageLink.AppendSegment(string pageName, Type? pageType, object? parameters)
	{
		return AppendSegmentImplied(pageName, pageType, parameters);
	}

	private PageLink AppendSegmentImplied(string pageName, Type? pageType, object? parameters)
	{
		if (string.IsNullOrEmpty(pageName) == false)
		{
			_pages.Add(new PageWithQuery(pageName, pageType)
			{
				Parameters = parameters
			});
		}
		return this;
	}

	public INavigationService NavigationService { get; private set; } = navigationService;

	string IPageLink.FullPath => string.Join('/', _pages.Select(p => p.GetResolvedName()));

	IEnumerable<Type?> IPageLink.PageTypes => _pages.Select(p => p.PageType);
}
