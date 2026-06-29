using Nkraft.MvvmEssentials.ViewModels;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials.Services;

public interface IContentViewFactory : IDisposable
{
    ContentView CreateView<TContentView, TViewModel>()
        where TContentView : ContentView
        where TViewModel : BaseViewModel;
}

internal sealed class ContentViewFactory(IServiceProvider serviceProvider) : IContentViewFactory
{
    private readonly Dictionary<ContentView, IServiceScope> _viewScopes = [];

    ContentView IContentViewFactory.CreateView<TContentView, TViewModel>()
    {
        var view = Activator.CreateInstance<TContentView>();

        var scope = serviceProvider.CreateScope();
        _viewScopes[view] = scope;
        view.BindingContext = scope.ServiceProvider.GetRequiredService<TViewModel>();

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
