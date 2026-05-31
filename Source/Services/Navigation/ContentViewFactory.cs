using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IContentViewFactory : IDisposable
{
    ContentView CreateView<TContentView, TViewModel>()
        where TContentView : ContentView
        where TViewModel : WizardStepViewModel;
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
        foreach (var (_, scope) in _viewScopes)
        {
            scope.Dispose();
        }
        _viewScopes.Clear();
    }
}
