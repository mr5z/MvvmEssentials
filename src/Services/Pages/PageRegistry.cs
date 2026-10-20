using System.Diagnostics.CodeAnalysis;
using Nkraft.MvvmEssentials.ViewModels;

// ReSharper disable once CheckNamespace
namespace Nkraft.MvvmEssentials.Services;

public interface IPageRegistry
{
	/// <summary>
	/// Maps a page type to a ViewModel type and registers both with the DI container.
	/// The page and ViewModel must follow the naming convention: <c>{Name}Page</c> and <c>{Name}ViewModel</c>.
	/// </summary>
	/// <typeparam name="TPage">The page type to register. Needs a public parameterless constructor.</typeparam>
	/// <typeparam name="TViewModel">The ViewModel type to bind to the page.</typeparam>
	/// <param name="isInitial">
	/// If <c>true</c>, this page is used as the app's entry point when no <see cref="IAppStartup"/>
	/// implementation is found. Only one page may be marked as initial.
	/// </param>
	IPageRegistry MapPage<
		TPage,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(bool isInitial = false)
		where TViewModel : PageViewModel
		where TPage : Page, new();
	
	/// <summary>
	/// Registers a ViewModel that is not directly mapped to a page — for example, a tab or flyout menu —
	/// with the correct scoped lifetime in the DI container.
	/// Use this for any ViewModel bound via XAML rather than through the navigation service.
	/// </summary>
	/// <typeparam name="TViewModel">The ViewModel type to register.</typeparam>
	IPageRegistry RegisterPage<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>()
		where TViewModel : BaseViewModel;

	/// <summary>
	/// Maps a <see cref="ContentView"/> type to a ViewModel type and registers the ViewModel with a
	/// scoped lifetime. Lets a ViewModel library request a view by ViewModel type alone
	/// (<see cref="IContentViewFactory.CreateView{TViewModel}"/>) without referencing the view's assembly.
	/// No naming convention applies.
	/// </summary>
	/// <typeparam name="TView">The view type to create. Needs a public parameterless constructor.</typeparam>
	/// <typeparam name="TViewModel">The ViewModel type bound to the view. One view per ViewModel.</typeparam>
	IPageRegistry MapView<
		TView,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>()
		where TView : ContentView, new()
		where TViewModel : BaseViewModel;
}

/// <summary>
/// The library's own view of what <see cref="IPageRegistry"/> registered: looks pages up and creates pages and views
/// from factories captured at registration, so no type is constructed by reflection (safe under trimming and
/// native AOT).
/// </summary>
internal interface IPageCatalog
{
	/// <summary>Returns the ViewModel type mapped to <paramref name="pageType"/>, or <c>null</c>.</summary>
	Type? ResolveViewModelType(Type pageType);

	/// <summary>
	/// Returns the registered page type whose name is <paramref name="pageName"/> (or <see cref="NavigationPage"/>),
	/// or <c>null</c>.
	/// </summary>
	Type? ResolvePageType(string pageName);

	/// <summary>Creates the page registered for <paramref name="pageType"/>.</summary>
	/// <exception cref="InvalidOperationException">No page is registered for <paramref name="pageType"/>.</exception>
	Page CreatePage(Type pageType);

	/// <summary>Creates the view mapped to <paramref name="viewModelType"/>, or returns <c>null</c> if none is.</summary>
	ContentView? CreateView(Type viewModelType);
}

internal sealed class PageRegistry(IServiceCollection services) : IPageRegistry, IPageCatalog
{
	private readonly IServiceCollection _services = services;

	private readonly Dictionary<Type, Type> _mappings = [];

	// NavigationPage needs no MapPage: ResolvePageType returns it for paths like "NavigationPage/MainPage".
	private readonly Dictionary<Type, Func<Page>> _pageFactories = new()
	{
		[typeof(NavigationPage)] = static () => new NavigationPage()
	};

	// Keyed by ViewModel type: views are requested by ViewModel, the reverse of _mappings. The view type is kept
	// for the duplicate-mapping message.
	private readonly Dictionary<Type, (Type ViewType, Func<ContentView> Create)> _views = [];

	IPageRegistry IPageRegistry.MapPage<
		TPage,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>(bool isInitial)
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
		_pageFactories[typeof(TPage)] = static () => new TPage();
		_services.AddScoped<TViewModel>();

		return this;
	}

	IPageRegistry IPageRegistry.RegisterPage<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>()
	{
		_services.AddScoped<TViewModel>();
		return this;
	}

	IPageRegistry IPageRegistry.MapView<
		TView,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TViewModel>()
	{
		if (_views.TryGetValue(typeof(TViewModel), out var existing))
		{
			throw new InvalidOperationException(
				$"The ViewModel type '{typeof(TViewModel).FullName}' is already mapped to view '{existing.ViewType.FullName}'.");
		}

		_views[typeof(TViewModel)] = (typeof(TView), static () => new TView());
		_services.AddScoped<TViewModel>();

		return this;
	}

	Type? IPageCatalog.ResolveViewModelType(Type pageType)
	{
		return _mappings.GetValueOrDefault(pageType);
	}

	Type? IPageCatalog.ResolvePageType(string pageName)
	{
		return pageName == nameof(NavigationPage)
			? typeof(NavigationPage)
			: _mappings.Keys.SingleOrDefault(t => t.Name == pageName);
	}

	Page IPageCatalog.CreatePage(Type pageType)
	{
		return _pageFactories.TryGetValue(pageType, out var create)
			? create()
			: throw new InvalidOperationException(
				$"No page is registered for '{pageType.FullName}'. Call MapPage in ConfigureMvvmEssentials.");
	}

	ContentView? IPageCatalog.CreateView(Type viewModelType)
	{
		return _views.TryGetValue(viewModelType, out var view) ? view.Create() : null;
	}

	// Only for the one-initial-page check in MapPage: the source generator reads isInitial from the MapPage call.
	private Type? _initialViewModelType;
}
