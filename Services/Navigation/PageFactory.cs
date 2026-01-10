using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nkraft.CrossUtility.Extensions;
using Nkraft.CrossUtility.Helpers;
using Nkraft.MvvmEssentials.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nkraft.MvvmEssentials.Services.Navigation;

internal interface IPageFactory
{
	event EventHandler<Page>? PageUnloaded;

	PageInfo[] GetPageTypesFromPath<TBasePage>(string path) where TBasePage : Page;

	Page CreatePage(PageInfo pageInfo, INavigationParameters? parameters = null);
}

internal class PageFactory(
	IOptions<NavigationOptions> options,
	ILogger<PageFactory> logger,
	IPageRegistry pageRegistry,
	IServiceProvider serviceProvider) : IPageFactory
{
	private readonly Assembly? _assemblyPageSource = options.Value.AssemblyPageSource;
	private readonly ILogger<PageFactory> _logger = logger;
	private readonly IPageRegistry _pageRegistry = pageRegistry;
	private readonly IServiceProvider _serviceProvider = serviceProvider;

	public event EventHandler<Page>? PageUnloaded;

	// TODO implement page reuse strategy if needed.
	Page IPageFactory.CreatePage(PageInfo pageInfo, INavigationParameters? parameters)
	{
		var page = (Page?)Activator.CreateInstance(pageInfo.PageType)
			?? throw new InvalidOperationException(
				$"Could not create instance of page type '{pageInfo.PageType.FullName}'. " +
				"Make sure the page is registered in the DI container and you are using the correct service.");

		var viewModelType = _pageRegistry.ResolveViewModelType(pageInfo.PageType);
		if (viewModelType is not null)
		{
			var viewModel = _serviceProvider.GetRequiredService(viewModelType);
			page.BindingContext = viewModel;

			if (viewModel is NavigableEntryViewModel baseViewModel)
			{
				foreach (var param in pageInfo.Parameters ?? [])
				{
					baseViewModel.SetNavigationParameter(param.Key, param.Value);
				}

				foreach (var param in parameters ?? new NavigationParameters())
				{
					baseViewModel.SetNavigationParameter(param.Key, param.Value);
				}
			}

			if (viewModel is IParameterSetAware parameterSetAware)
			{
				parameterSetAware.OnParametersSet(parameters ?? new NavigationParameters());
			}
		}

		RegisterPageEvents(page);

		return page;
	}

	PageInfo[] IPageFactory.GetPageTypesFromPath<TBasePage>(string path)
	{
		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return [.. segments.Select(segment =>
		{
			var parts = segment.Split('?', StringSplitOptions.RemoveEmptyEntries);
			var pageName = parts[0];
			var queryParameters = parts.Length > 1 ? parts[1] : string.Empty;
			var queryDictionary = QueryStringHelper.ToDictionary(queryParameters);
			var pageType = FindPageTypeByName(pageName, typeof(TBasePage));
			return pageType == null
				? throw new InvalidOperationException($"Page '{pageName}' not found.")
				: new PageInfo(pageType, queryDictionary);
		})];
	}

	private Type? FindPageTypeByName(string pageName, Type basePage)
	{
		var pageDictionary = GetAssemblyPageSourceTypes(basePage);
		if (pageDictionary.TryGetValue(pageName, out var type))
		{
  			return type;
		}
		return null;
	}

	private Dictionary<string, Type>? _cachedAssemblyPageSourceTypes;
	private Dictionary<string, Type> GetAssemblyPageSourceTypes(Type basePage)
	{
		if (_cachedAssemblyPageSourceTypes is null)
		{
			var types = _assemblyPageSource?.GetTypes()
				.Where(pageType => pageType.IsSubclassOf(basePage))
				.Where(pageType => pageType.IsAbstract == false)
				.Where(pageType => pageType.IsGenericType == false)
				.Concat([typeof(NavigationPage)])
				.ToDictionary(t => t.Name, t => t) ?? [];
			_cachedAssemblyPageSourceTypes = types;
		}
		return _cachedAssemblyPageSourceTypes;
	}

	private void RegisterPageEvents(Page? page)
	{
		if (page is not null)
		{
			page.Appearing += Page_Appearing;
			page.Disappearing += Page_Disappearing;
			page.NavigatedTo += Page_NavigatedTo;
			page.NavigatedFrom += Page_NavigatedFrom;
			page.Unloaded += Page_Unloaded;
		}
	}

	private void UnregisterPageEvents(Page? page)
	{
		if (page is not null)
		{
			page.Appearing -= Page_Appearing;
			page.Disappearing -= Page_Disappearing;
			page.NavigatedTo -= Page_NavigatedTo;
			page.NavigatedFrom -= Page_NavigatedFrom;
			page.Unloaded -= Page_Unloaded;
		}
	}

	private void Page_Appearing(object? sender, EventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is IAppearingAware appearingAware)
			{
				appearingAware.OnPageAppearing();
			}

			if (viewModel is IAppearingAwareAsync appearingAwareAsync)
			{
				appearingAwareAsync.OnPageAppearingAsync().FireAndForget(exception =>
				{
					_logger.LogError(exception,
						"An error occurred while trying to invoke {MethodName}.",
						nameof(appearingAwareAsync.OnPageAppearingAsync)
					);
				});
			}
		}
	}

	private void Page_Disappearing(object? sender, EventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is IAppearingAware appearingAware)
			{
				appearingAware.OnPageDisappearing();
			}

			if (viewModel is IAppearingAwareAsync appearingAwareAsync)
			{
				appearingAwareAsync.OnPageDisappearingAsync().FireAndForget(exception =>
				{
					_logger.LogError(exception,
						"An error occurred while trying to invoke {MethodName}.",
						nameof(appearingAwareAsync.OnPageDisappearingAsync)
					);
				});
			}
		}
	}

	private void Page_NavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is INavigatedAware navigatedAware)
			{
				navigatedAware.OnNavigatedTo();
			}
		}
	}

	private void Page_NavigatedFrom(object? sender, NavigatedFromEventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is INavigatedAware navigatedAware)
			{
				navigatedAware.OnNavigatedFrom();
			}
		}
	}

	private void Page_Unloaded(object? sender, EventArgs e)
	{
		if (sender is Page page && page.BindingContext is object viewModel)
		{
			if (viewModel is IPageLoadAware loadAware)
			{
				loadAware.OnPageUnloaded();
				UnregisterPageEvents(page);
				PageUnloaded?.Invoke(this, page);
			}
		}
	}

	private static bool TryGetViewModel(object? sender, [NotNullWhen(true)] out object? resultViewModel)
	{
		if (sender is Page { BindingContext: { } viewModel })
		{
			resultViewModel = viewModel;
			return true;
		}
		resultViewModel = null;
		return false;
	}
}
