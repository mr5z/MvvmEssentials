using Nkraft.CrossUtility.Patterns;

namespace Nkraft.MvvmEssentials.Services.Pages;

internal enum NavigationAction
{
    Completed,

    ContinueInto
}

internal record NavigationContext(NavigationAction Action, Page? NextPage = null)
{
    public static NavigationContext Complete() => new(NavigationAction.Completed);

    public static NavigationContext Into(Page page) => new(NavigationAction.ContinueInto, page);
}

internal sealed class NavigationRequest(
    PageInfo[] pages,
    INavigationParameters parameters,
    IPageFactory pageFactory)
{
    private readonly List<Page> _created = [];
    
    public IReadOnlyList<PageInfo> Pages { get; } = pages;

    public INavigationParameters Parameters { get; } = parameters;

    public Page Materialize(PageInfo pageInfo)
    {
        var page = pageFactory.CreatePage(pageInfo, Parameters);
        _created.Add(page);
        return page;
    }

    public IReadOnlyList<Page> MaterializeAll() => [.. Pages.Select(Materialize)];
    
    /// <summary>
    /// Disposes the scope of every page this request created that isn't reachable from
    /// <paramref name="root"/> — those never entered the tree, so Unloaded will never fire.
    /// </summary>
    public AggregateException? ReleaseUnreachablePages(Page? root)
    {
        List<Exception> failures = [];
        
        foreach (var page in _created)
        {
            if (IsReachableFrom(page, root))
                continue;

            try
            {
                pageFactory.ReleasePage(page);
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException($"Failed to release page '{page.GetType().Name}'.", ex));
            }
        }

        _created.Clear();

        return failures.Count > 0 ? new AggregateException(failures) : null;
    }

    private static bool IsReachableFrom(Page page, Page? root)
    {
        for (Element? current = page; current is not null; current = current.Parent)
        {
            if (ReferenceEquals(current, root))
                return true;
        }

        return false;
    }
}

internal interface IPageNavigationHandler
{
    Task<Result<NavigationContext>> HandleAsync(Page page, NavigationRequest request, bool animated);
}
