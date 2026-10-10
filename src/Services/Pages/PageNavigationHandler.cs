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
    private readonly PageLease _lease = new(pageFactory);
    
    public IReadOnlyList<PageInfo> Pages { get; } = pages;

    public INavigationParameters Parameters { get; } = parameters;

    public Page Materialize(PageInfo pageInfo) => _lease.Create(pageInfo, Parameters);

    public IReadOnlyList<Page> MaterializeAll() => [.. Pages.Select(Materialize)];
    
    /// <summary>
    /// Disposes the scope of every page this request created that isn't reachable from
    /// <paramref name="root"/> — those never entered the tree, so Unloaded will never fire.
    /// </summary>
    public AggregateException? ReleaseUnreachablePages(Page? root) => _lease.ReleaseUnreachable(root);
}

internal interface IPageNavigationHandler
{
    Task<Result<NavigationContext>> HandleAsync(Page page, NavigationRequest request, bool animated);
}