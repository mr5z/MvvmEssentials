using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nkraft.MvvmEssentials.Services.Pages;

/// <summary>
/// Owns the pages created during a single navigation operation.
/// <para>
/// Pages that never enter the visual tree never raise <c>Unloaded</c>, so their DI scope would leak.
/// The lease releases every page it created that isn't reachable from the committed root.
/// </para>
/// <para>
/// This is the only type that should call <see cref="IPageFactory.ReleasePage"/>.
/// </para>
/// </summary>
internal sealed class PageLease(IPageFactory pageFactory, ILogger? logger = null) : IDisposable
{
    private readonly IPageFactory _pageFactory = pageFactory;
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly List<Page> _created = [];
    private Page? _root;

    public Page Create(PageInfo pageInfo, INavigationParameters? parameters)
    {
        var page = _pageFactory.CreatePage(pageInfo, parameters);
        _created.Add(page);
        return page;
    }

    /// <summary>
    /// Marks <paramref name="root"/> as presented; pages reachable from it are kept on <see cref="Dispose"/>.
    /// </summary>
    public void Commit(Page root) => _root = root;

    /// <summary>
    /// Disposes the scope of every page this lease created that isn't reachable from
    /// <paramref name="root"/> — those never entered the tree, so Unloaded will never fire.
    /// </summary>
    public AggregateException? ReleaseUnreachable(Page? root)
    {
        List<Exception> failures = [];

        foreach (var page in _created)
        {
            if (IsReachableFrom(page, root))
                continue;

            try
            {
                _pageFactory.ReleasePage(page);
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException($"Failed to release page '{page.GetType().Name}'.", ex));
            }
        }

        _created.Clear();

        return failures.Count > 0 ? new AggregateException(failures) : null;
    }

    void IDisposable.Dispose()
    {
        if (ReleaseUnreachable(_root) is {} ex)
        {
            const string message = "One or more page scopes failed to dispose.";
            _logger.LogWarning(ex, message);
        }
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