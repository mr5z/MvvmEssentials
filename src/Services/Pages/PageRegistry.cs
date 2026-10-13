using Nkraft.MvvmEssentials.ViewModels;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials.Services;

public interface IPageRegistry
{
	/// <summary>
	/// Maps a page type to a ViewModel type and registers both with the DI container.
	/// The page and ViewModel must follow the naming convention: <c>{Name}Page</c> and <c>{Name}ViewModel</c>.
	/// </summary>
	/// <typeparam name="TPage">The page type to register.</typeparam>
	/// <typeparam name="TViewModel">The ViewModel type to bind to the page.</typeparam>
	/// <param name="isInitial">
	/// If <c>true</c>, this page is used as the app's entry point when no <see cref="IAppStartup"/>
	/// implementation is found. Only one page may be marked as initial.
	/// </param>
	IPageRegistry MapPage<TPage, TViewModel>(bool isInitial = false)
		where TViewModel : PageViewModel
		where TPage : Page;
	
	/// <summary>
	/// Registers a ViewModel that is not directly mapped to a page — for example, a tab or flyout menu —
	/// with the correct scoped lifetime in the DI container.
	/// Use this for any ViewModel bound via XAML rather than through the navigation service.
	/// </summary>
	/// <typeparam name="TViewModel">The ViewModel type to register.</typeparam>
	IPageRegistry RegisterPage<TViewModel>() where TViewModel : BaseViewModel;

	/// <summary>
	/// Maps a <see cref="ContentView"/> type to a ViewModel type and registers the ViewModel with a
	/// scoped lifetime. Lets a ViewModel library request a view by ViewModel type alone
	/// (<see cref="IContentViewFactory.CreateView{TViewModel}"/>) without referencing the view's assembly.
	/// No naming convention applies.
	/// </summary>
	/// <typeparam name="TView">The view type to create.</typeparam>
	/// <typeparam name="TViewModel">The ViewModel type bound to the view. One view per ViewModel.</typeparam>
	IPageRegistry MapView<TView, TViewModel>()
		where TView : ContentView
		where TViewModel : BaseViewModel;

	Type? ResolveViewModelType(Type pageType);
	
	Type? ResolvePageType(string pageName);

	/// <summary>Returns the view type mapped via <see cref="MapView{TView,TViewModel}"/>, or <c>null</c>.</summary>
	Type? ResolveViewType(Type viewModelType);
	
	Type? InitialViewModelType { get; }
}

internal sealed class PageRegistry(IServiceCollection services) : IPageRegistry
{
	private readonly IServiceCollection _services = services;

	private readonly Dictionary<Type, Type> _mappings = [];
	
	// Keyed by ViewModel type: views are requested by ViewModel, the reverse of _mappings.
	private readonly Dictionary<Type, Type> _viewMappings = [];

	IPageRegistry IPageRegistry.MapPage<TPage, TViewModel>(bool isInitial)
	{
		if (_mappings.ContainsKey(typeof(TPage)))
		{
			throw new InvalidOperationException(
				$"The page type '{typeof(TPage).FullName}' is already registered.");
		}
		
		if (isInitial)
		{
			if (_initialViewModelType is not null)
				throw new InvalidOperationException(
					$"An initial page is already registered: '{_initialViewModelType.FullName}'. " +
					$"Only one page can be marked with isInitial: true.");

			_initialViewModelType = typeof(TViewModel);
		}

		_mappings[typeof(TPage)] = typeof(TViewModel);
		_services.AddScoped<TViewModel>();

		return this;
	}

	IPageRegistry IPageRegistry.RegisterPage<TViewModel>()
	{
		_services.AddScoped<TViewModel>();
		return this;
	}

	IPageRegistry IPageRegistry.MapView<TView, TViewModel>()
	{
		if (_viewMappings.TryGetValue(typeof(TViewModel), out var existing))
		{
			throw new InvalidOperationException(
				$"The ViewModel type '{typeof(TViewModel).FullName}' is already mapped to view '{existing.FullName}'.");
		}

		_viewMappings[typeof(TViewModel)] = typeof(TView);
		_services.AddScoped<TViewModel>();

		return this;
	}

	Type? IPageRegistry.ResolveViewModelType(Type pageType)
	{
		return _mappings.GetValueOrDefault(pageType);
	}

	Type? IPageRegistry.ResolvePageType(string pageName)
	{
		return pageName == nameof(NavigationPage)
			? typeof(NavigationPage)
			: _mappings.Keys.SingleOrDefault(t => t.Name == pageName);
	}

	Type? IPageRegistry.ResolveViewType(Type viewModelType)
	{
		return _viewMappings.GetValueOrDefault(viewModelType);
	}
	
	private Type? _initialViewModelType;
	Type? IPageRegistry.InitialViewModelType => _initialViewModelType;
}
