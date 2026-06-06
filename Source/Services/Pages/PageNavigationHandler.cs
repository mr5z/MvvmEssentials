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
    public IReadOnlyList<PageInfo> Pages { get; } = pages;

    public INavigationParameters Parameters { get; } = parameters;

    public Page Materialize(PageInfo pageInfo) => pageFactory.CreatePage(pageInfo, Parameters);

    public IReadOnlyList<Page> MaterializeAll() => [.. Pages.Select(Materialize)];
}

internal interface IPageNavigationHandler
{
    bool CanHandle(Page? page);

    Task<Result<NavigationContext>> HandleAsync(Page page, NavigationRequest request, bool animated);
}
