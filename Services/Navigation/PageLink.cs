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

	public PageLink(INavigationService navigationService, string? rootPage) : this(navigationService)
	{
		AppendSegmentImplied(rootPage, null, null);
	}

	IPageLink IPageLink.AppendSegment(string? pageName, object? parameters)
	{
		AppendSegmentImplied(pageName, null, parameters);
		return this;
	}

	IPageLink IPageLink.AppendSegment(string? pageName, Type? pageType, object? parameters)
	{
		AppendSegmentImplied(pageName, pageType, parameters);
		return this;
	}

	private void AppendSegmentImplied(string? pageName, Type? pageType, object? parameters)
	{
		if (string.IsNullOrEmpty(pageName) == false)
		{
			_pages.Add(new PageWithQuery(pageName, pageType)
			{
				Parameters = parameters
			});
		}
	}

	public INavigationService NavigationService { get; private set; } = navigationService;

	string IPageLink.FullPath => string.Join('/', _pages.Select(p => p.GetResolvedName()));

	IEnumerable<Type?> IPageLink.PageTypes => _pages.Select(p => p.PageType);
}
