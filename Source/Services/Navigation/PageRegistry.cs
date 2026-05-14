using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Navigation;

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

	Type? ResolveViewModelType(Type pageType);
	
	Type? InitialViewModelType { get; }
}

internal sealed class PageRegistry(IServiceCollection services) : IPageRegistry
{
	private readonly IServiceCollection _services = services;

	private readonly Dictionary<Type, Type> _mappings = [];

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

	Type? IPageRegistry.ResolveViewModelType(Type pageType)
	{
		return _mappings.GetValueOrDefault(pageType);
	}
	
	private Type? _initialViewModelType;
	Type? IPageRegistry.InitialViewModelType => _initialViewModelType;
}
