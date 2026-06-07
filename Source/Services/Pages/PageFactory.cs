using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Extensions;
using Nkraft.CrossUtility.Helpers;
using Nkraft.MvvmEssentials.Services.Helpers;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Pages;

internal interface IPageFactory
{
	event EventHandler<Page>? PageUnloaded;

	PageInfo[] GetPageTypesFromPath<TBasePage>(string path) where TBasePage : Page;

	Page CreatePage(PageInfo pageInfo, INavigationParameters? parameters = null);
}

internal class PageFactory(
	ILogger<PageFactory> logger,
	IPageRegistry pageRegistry,
	IServiceProvider serviceProvider,
	IDispatcher dispatcher) : IPageFactory
{
	private readonly ILogger<PageFactory> _logger = logger;
	private readonly IPageRegistry _pageRegistry = pageRegistry;
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly IDispatcher _dispatcher = dispatcher;
	private readonly Dictionary<Page, IServiceScope> _pageScopes = [];

	public event EventHandler<Page>? PageUnloaded;

	// TODO implement page reuse strategy if needed.
	Page IPageFactory.CreatePage(PageInfo pageInfo, INavigationParameters? parameters)
	{
		var page = Activator.CreateInstance(pageInfo.PageType) as Page
			?? throw new InvalidOperationException(
				$"Could not create instance of page type '{pageInfo.PageType.FullName}'. " +
				"Make sure the page is registered in the DI container and you are using the correct service.");
		
		var scope = _serviceProvider.CreateScope();
		_pageScopes[page] = scope;

		var viewModelType = _pageRegistry.ResolveViewModelType(pageInfo.PageType);
		if (viewModelType is not null)
		{
			var viewModel = scope.ServiceProvider.GetRequiredService(viewModelType);
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
			var pageType = _pageRegistry.ResolvePageType(pageName);
			return pageType is null
				? throw new InvalidOperationException($"Page '{pageName}' not found.")
				: new PageInfo(pageType, queryDictionary);
		})];
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
			if (viewModel is IPageAppearingAware appearingAware)
			{
				appearingAware.OnPageAppearing();
			}

			if (viewModel is IPageAppearingAwareAsync appearingAwareAsync)
			{
				appearingAwareAsync.OnPageAppearingAsync().FireAndForget(exception =>
				{
					ExceptionDispatcher.Handle(
						exception,
						_logger, 
						_dispatcher, 
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
			if (viewModel is IPageAppearingAware appearingAware)
			{
				appearingAware.OnPageDisappearing();
			}

			if (viewModel is IPageAppearingAwareAsync appearingAwareAsync)
			{
				appearingAwareAsync.OnPageDisappearingAsync().FireAndForget(exception =>
				{
					ExceptionDispatcher.Handle(
						exception,
						_logger, 
						_dispatcher, 
						nameof(appearingAwareAsync.OnPageDisappearingAsync)
					);
				});
			}
		}
	}

	private static void Page_NavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is INavigatedAware navigatedAware)
			{
				navigatedAware.OnNavigatedTo();
			}
		}
	}

	private static void Page_NavigatedFrom(object? sender, NavigatedFromEventArgs e)
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
		if (sender is not Page page)
		{
			const string error = "Received a page unloaded event but the sender '{ActualType}' is not a Page";
			_logger.LogWarning(error, sender?.GetType().Name);
			return;
		}
		
		if (page.BindingContext is IPageLoadAware loadAware)
		{
			loadAware.OnPageUnloaded();
		}
		
		UnregisterPageEvents(page);
		PageUnloaded?.Invoke(this, page);

		if (_pageScopes.Remove(page, out var scope))
		{
			scope.Dispose();
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
