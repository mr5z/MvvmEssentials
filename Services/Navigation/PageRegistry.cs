using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Navigation;

public interface IPageRegistry
{
	IPageRegistry MapPage<TPage, TViewModel>()
		where TViewModel : PageViewModel
		where TPage : Page;

	Type? ResolveViewModelType(Type pageType);
}

internal sealed class PageRegistry(IServiceCollection services) : IPageRegistry
{
	private readonly IServiceCollection _services = services;

	private readonly Dictionary<Type, Type> _mappings = [];

	IPageRegistry IPageRegistry.MapPage<TPage, TViewModel>()
	{
		if (_mappings.ContainsKey(typeof(TPage)))
		{
			throw new InvalidOperationException($"The page type '{typeof(TPage).FullName}' is already registered.");
		}

		_mappings[typeof(TPage)] = typeof(TViewModel);
		_services.AddTransient<TViewModel>();

		return this;
	}

	Type? IPageRegistry.ResolveViewModelType(Type pageType)
	{
		if (_mappings.TryGetValue(pageType, out var viewModel))
		{
			return viewModel;
		}
		return null;
	}
}
