using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Helpers;
using Nkraft.MvvmEssentials.Services.Dispatchers;
using Nkraft.MvvmEssentials.Services.Pages.Lifecycles;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Services.Pages;

internal interface IPageFactory
{
	event EventHandler<Page>? PageUnloaded;

	PageInfo[] GetPageTypesFromPath(string path);

	Page CreatePage(PageInfo pageInfo, INavigationParameters? parameters = null);

	void ReleasePage(Page page);
}

internal class PageFactory(
	ILogger<PageFactory> logger,
	IPageRegistry pageRegistry,
	IServiceProvider serviceProvider,
	ILifecycleExceptionHandler exceptionHandler) : IPageFactory
{
	private readonly ILogger<PageFactory> _logger = logger;
	private readonly IPageRegistry _pageRegistry = pageRegistry;
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ILifecycleExceptionHandler _exceptionHandler = exceptionHandler;
	private readonly ConditionalWeakTable<Page, IServiceScope> _pageScopes = [];

	public event EventHandler<Page>? PageUnloaded;

	// TODO implement page reuse strategy if needed.
	Page IPageFactory.CreatePage(PageInfo pageInfo, INavigationParameters? parameters)
	{
		var page = Activator.CreateInstance(pageInfo.PageType) as Page
			?? throw new InvalidOperationException(
				$"Could not create instance of page type '{pageInfo.PageType.FullName}'. " +
				"Make sure the page is registered in the DI container and you are using the correct service.");
		
		var scope = _serviceProvider.CreateScope();
		
		try
		{
			var viewModelType = _pageRegistry.ResolveViewModelType(pageInfo.PageType);
			if (viewModelType is not null)
			{
				var viewModel = scope.ServiceProvider.GetRequiredService(viewModelType);
				page.BindingContext = viewModel;
				
				var mergedParameters = MergeParameters(pageInfo.Parameters, parameters);
				if (viewModel is NavigableEntryViewModel baseViewModel)
				{
					foreach (var parameter in mergedParameters)
					{
						baseViewModel.SetNavigationParameter(parameter.Key, parameter.Value);
					}
				}

				if (viewModel is IParametersSet vm)
				{
					vm.OnParametersSet(mergedParameters);
				}

			}

			RegisterPageEvents(page);
			_pageScopes.Add(page, scope);

			return page;
		}
		catch (Exception ex)
		{
			const string message = "Unable to create instance of page '{PageName}'.";
			_logger.LogError(ex, message, pageInfo.PageType.Name);
			scope.Dispose();
			throw;
		}
	}

	PageInfo[] IPageFactory.GetPageTypesFromPath(string path)
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
	
	void IPageFactory.ReleasePage(Page page)
	{
		UnregisterPageEvents(page);
		DisposeScope(page);
	}
	
	private static NavigationParameters MergeParameters(
		Dictionary<string, object>? segmentParameters,
		INavigationParameters? parameters)
	{
		var merged = new NavigationParameters();

		foreach (var (key, value) in segmentParameters ?? [])
		{
			merged[key] = value;
		}

		foreach (var (key, value) in parameters ?? new NavigationParameters())
		{
			merged[key] = value;
		}

		return merged;
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

	// Lifecycle methods are user code running inside MAUI's own navigation (e.g. NavigationPage's
	// CurrentPage setter raises Appearing). An exception must never unwind MAUI mid-transition, so every
	// call goes through the lifecycle exception handler, which applies the configured behavior afterwards.
	private void Page_Appearing(object? sender, EventArgs e)
	{
		if (sender is Page page)
		{
			HandlePageAppearing(page);
		}
	}

	// Internally exposed to simulate MAUI raising Appearing, which needs a window attached to an Application.
	internal void HandlePageAppearing(Page page)
	{
		if (TryGetViewModel(page, out var viewModel) && viewModel is IPageAppearing vm)
		{
			_exceptionHandler.Invoke(vm.OnPageAppearing, nameof(IPageAppearing.OnPageAppearing), vm);
			_exceptionHandler.InvokeAsync(vm.OnPageAppearingAsync, nameof(IPageAppearing.OnPageAppearingAsync), vm);
		}
	}

	private void Page_Disappearing(object? sender, EventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel) && viewModel is IPageAppearing vm)
		{
			_exceptionHandler.Invoke(vm.OnPageDisappearing, nameof(IPageAppearing.OnPageDisappearing), vm);
			_exceptionHandler.InvokeAsync(vm.OnPageDisappearingAsync, nameof(IPageAppearing.OnPageDisappearingAsync), vm);
		}
	}

	private void Page_NavigatedTo(object? sender, NavigatedToEventArgs e)
	{
		if (sender is Page page)
			HandlePageNavigatedTo(page);
	}

	// Internally exposed to simulate MAUI raising NavigatedTo, which a NavigationPage without a handler doesn't do.
	internal void HandlePageNavigatedTo(Page page)
	{
		if (TryGetViewModel(page, out var viewModel) && viewModel is IPageNavigated vm)
		{
			_exceptionHandler.Invoke(vm.OnPageNavigatedTo, nameof(IPageNavigated.OnPageNavigatedTo), vm);
			_exceptionHandler.InvokeAsync(vm.OnPageNavigatedToAsync, nameof(IPageNavigated.OnPageNavigatedToAsync), vm);
		}
	}

	private void Page_NavigatedFrom(object? sender, NavigatedFromEventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel) && viewModel is IPageNavigated vm)
		{
			_exceptionHandler.Invoke(vm.OnPageNavigatedFrom, nameof(IPageNavigated.OnPageNavigatedFrom), vm);
			_exceptionHandler.InvokeAsync(vm.OnPageNavigatedFromAsync, nameof(IPageNavigated.OnPageNavigatedFromAsync), vm);
		}
	}

	private void Page_Unloaded(object? sender, EventArgs e)
	{
		if (sender is not Page page)
		{
			const string message = "Received a page unloaded event but the sender '{ActualType}' is not a Page";
			_logger.LogWarning(message, sender?.GetType().Name);
			return;
		}
		
		HandlePageUnloaded(page);
	}
	
	// This has been internally exposed to simulate page unload event from MAUI
	internal void HandlePageUnloaded(Page page)
	{
		// Guarded so a throwing OnPageUnloaded can't skip unsubscribing and disposing the scope.
		if (page.BindingContext is IPageLoad vm)
		{
			_exceptionHandler.Invoke(vm.OnPageUnloaded, nameof(IPageLoad.OnPageUnloaded), vm);
		}
		
		UnregisterPageEvents(page);
		PageUnloaded?.Invoke(this, page);
		DisposeScope(page);
	}
	
	private void DisposeScope(Page page)
	{
		if (_pageScopes.TryGetValue(page, out var scope))
		{
			_pageScopes.Remove(page);
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