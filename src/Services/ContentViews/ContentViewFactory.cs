using Nkraft.MvvmEssentials.ViewModels;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials.Services;

public interface IContentViewFactory : IDisposable
{
    /// <summary>
    /// Creates the view mapped to <typeparamref name="TViewModel"/> via
    /// <see cref="IPageRegistry.MapView{TView,TViewModel}"/>, bound to a ViewModel resolved in its own DI scope.
    /// </summary>
    ContentView CreateView<TViewModel>() where TViewModel : BaseViewModel;
}

internal sealed class ContentViewFactory(IServiceProvider serviceProvider, IPageCatalog pageCatalog) : IContentViewFactory
{
    private readonly Dictionary<ContentView, IServiceScope> _viewScopes = [];

    ContentView IContentViewFactory.CreateView<TViewModel>()
    {
        var view = pageCatalog.CreateView(typeof(TViewModel))
            ?? throw new InvalidOperationException(
                $"No view is mapped to '{typeof(TViewModel).FullName}'. " +
                $"Call MapView<TView, {typeof(TViewModel).Name}>() in ConfigureMvvmEssentials.");

        var scope = serviceProvider.CreateScope();
        try
        {
            view.BindingContext = scope.ServiceProvider.GetRequiredService<TViewModel>();
        }
        catch
        {
            scope.Dispose();
            throw;
        }

        _viewScopes[view] = scope;
        return view;
    }

    void IDisposable.Dispose()
    {
        foreach (var (view, scope) in _viewScopes)
        {
            if (view.BindingContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
            scope.Dispose();
        }
        _viewScopes.Clear();
    }
}
