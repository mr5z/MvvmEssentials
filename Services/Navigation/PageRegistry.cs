using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IPageRegistry
{
	IPageRegistry MapPage<TPage, TViewModel>(bool isInitial = false)
		where TViewModel : PageViewModel
		where TPage : Page;

	Type? ResolveViewModelType(Type pageType);
	
	/// <summary>
	/// The ViewModel type marked with isInitial: true, if any.
	/// </summary>
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
		_services.AddTransient<TViewModel>();

		return this;
	}

	Type? IPageRegistry.ResolveViewModelType(Type pageType)
	{
		return _mappings.GetValueOrDefault(pageType);
	}
	
	private Type? _initialViewModelType;
	Type? IPageRegistry.InitialViewModelType => _initialViewModelType;
}
