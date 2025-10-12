using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nkraft.CrossUtility.Helpers;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nkraft.MvvmEssentials.Services;

/// <summary>
/// Defines navigation operations for navigating between pages in a .NET MAUI application.
/// </summary>
public interface INavigationService
{
	/// <summary>
	/// Navigates to the specified pages defined by the <paramref name="path"/> string.
	/// <para>
	/// - Page names must correspond to types that exist in the configured assembly (see <c>NavigationOptions.AssemblyPageSource</c>).
	///	</para>
	/// <para>
	/// - Use <c>//</c> at the start of the path to perform absolute navigation, which will completely replace the application's main page and navigation stack.
	/// </para>
	/// <para>
	/// - Use <c>NavigationPage</c> in the path to wrap subsequent pages (separated by '/') in a <see cref="NavigationPage"/>.
	/// </para>
	/// <para>
	/// - Query parameters can be appended to page names using <c>?</c> (e.g., <c>MyPage?param1=value1</c>).
	/// </para>
	/// <para>
	/// - Relative navigation (without <c>//</c>) will push pages onto the current navigation stack or the current tab's stack if within a <see cref="TabbedPage"/>.
	/// </para>
	/// </summary>
	/// <param name="path">Navigation path, e.g., "//MainPage/NavigationPage/DetailsPage".</param>
	/// <param name="parameters">Optional navigation parameters to pass to the target page's view model.</param>
	/// <param name="animated">Whether to animate the navigation transition.</param>
	/// <returns>An <see cref="IResult"/> indicating success or failure of the navigation operation.</returns>
	Task<IResult> NavigateAsync(string path, INavigationParameters? parameters = null, bool animated = true);

	/// <summary>
	/// Navigates back to the previous page in the navigation stack.
	/// <para>
	/// - If the current page is within a <see cref="NavigationPage"/>, this will pop the top page from the navigation stack.
	/// </para>
	/// <para>
	/// - If not within a <see cref="NavigationPage"/>, this will call <see cref="Page.SendBackButtonPressed()"/> on the current page.
	/// </para>
	/// </summary>
	/// <param name="animated">Whether to animate the navigation transition.</param>
	/// <returns>An <see cref="IResult"/> indicating success or failure of the navigation operation.</returns>
	Task<IResult> NavigateBackAsync(bool animated = true);

	/// <summary>
	/// Navigates to the root page of the navigation stack.
	/// <para>
	/// - If the root page implements <see cref="IRootPageAware"/> or <see cref="IRootPageAwareAsync"/>, the corresponding event will be delivered.
	/// </para>
	/// - Only supported when the current page is a <see cref="NavigationPage"/> or the current tab of a <see cref="TabbedPage"/> is a <see cref="NavigationPage"/>.
	/// </summary>
	/// <param name="parameters">Optional navigation parameters to pass to the root page's view model.</param>
	/// <param name="animated">Whether to animate the navigation transition.</param>
	/// <returns>An <see cref="IResult"/> indicating success or failure of the navigation operation.</returns>
	Task<IResult> NavigateToRootAsync(INavigationParameters? parameters = null, bool animated = true);
}

internal sealed class NavigationService : INavigationService
{
	private readonly ILogger<NavigationService> _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly IPageRegistry _pageRegistry;

	private readonly Assembly? _assemblyPageSource;

	public NavigationService(
		ILogger<NavigationService> logger,
		IServiceProvider serviceProvider,
		IPageRegistry pageRegistry,
		IWindowEventHandler windowHandler,
		IOptions<NavigationOptions> options)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
		_pageRegistry = pageRegistry;
		_assemblyPageSource = options.Value.AssemblyPageSource;

		windowHandler.Activated += WindowEvent_Activated;
	}

	// Handle support for FlyoutPage, and nested NavigationPage if needed in the future.
	async Task<IResult> INavigationService.NavigateAsync(string path, INavigationParameters? parameters, bool animated)
	{
		var currentApp = Application.Current;
		if (currentApp is null)
		{
			const string error = "Application.Current is null";
			_logger.LogError(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		PageInfo[] pageInfoList;

		try
		{
			pageInfoList = GetPageTypesFromPath(path);
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to fetch page information";
			_logger.LogError(ex, error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		if (pageInfoList.Length == 0)
		{
			const string error = "No valid pages found in the navigation path";
			_logger.LogError(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		try
		{
			// Build the page stack
			var pages = pageInfoList.Select(pageInfo =>
			{
				var viewModelType = _pageRegistry.ResolveViewModelType(pageInfo.PageType);
				var page = CreatePage(pageInfo, viewModelType, parameters);
				RegisterPageEvents(page);
				return page;
			}).ToImmutableArray();

			var replaceCurrentPage = path.StartsWith('/');

			if (replaceCurrentPage)
			{
				if (pages.Length == 0)
				{
					const string error = "No valid pages to navigate to";
					_logger.LogError(error);
					return Result.Fail(ErrorCode.InvalidState, error);
				}

				var firstPage = pages.First();
				if (firstPage is null)
				{
					const string error = "First page is null";
					_logger.LogError(error);
					return Result.Fail(ErrorCode.InvalidState, error);
				}

				Page mainPage;

				if (firstPage is TabbedPage tabbedPage)
				{
					if (tabbedPage.CurrentPage is NavigationPage tabNavigationPage)
					{
						await PushPagesAsync(tabNavigationPage, pages.Skip(1), animated);
						mainPage = tabbedPage;
					}
					else
					{
						return Result.Fail(ErrorCode.NotSupported, "Absolute navigation within a TabbedPage is only supported when the current tab is wrapped in NavigationPage");
					}
				}
				else if (firstPage is NavigationPage navigationPage)
				{
					await PushPagesAsync(navigationPage, pages.Skip(1), animated);
					mainPage = navigationPage;
				}
				else
				{
					if (pages.Length > 1)
					{
						navigationPage = new NavigationPage(firstPage);
						foreach (var page in pages.Skip(1))
						{
							await navigationPage.PushAsync(page, animated);
						}
						mainPage = navigationPage;
					}
					else
					{
						mainPage = firstPage;
					}
				}

#pragma warning disable CS0618 // Type or member is obsolete
				currentApp.MainPage = mainPage;
#pragma warning restore CS0618 // Type or member is obsolete
			}
			else
			{
				var currentPage = currentApp.Windows[0].Page;
				if (currentPage is null)
				{
					const string error = "Current page is null";
					_logger.LogError(error);
					return Result.Fail(ErrorCode.InvalidState, error);
				}
				return await HandleContextualNavigationAsync(currentPage, pages, animated);
			}
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to perform page navigation (Path: {Path})";
			_logger.LogError(ex, error, path);
			return Result.Fail(ErrorCode.Unknown, error);
		}

		return Result.Ok();
	}

	async Task<IResult> INavigationService.NavigateBackAsync(bool animated)
	{
		if (TryGetCurrentPage(out var currentPage) == false)
		{
			const string error = "Main page is not set for current window";
			_logger.LogError(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		if (currentPage is NavigationPage navigationPage)
		{
			var canPopPage = navigationPage.Navigation.NavigationStack.Count > 0;
			if (canPopPage)
			{
				const string error = "No page to navigate back to";
				_logger.LogError(error);
				return Result.Fail(ErrorCode.InvalidState, error);
			}

			var previousPage = await navigationPage.PopAsync(animated);
			if (previousPage is null)
			{
				const string error = "Popped page returns null";
				_logger.LogError(error);
				return Result.Fail(ErrorCode.InvalidState, error);
			}
			else
			{
				return Result.Ok();
			}
		}
		else if (currentPage is not null)
		{
			var navigatedBack = currentPage.SendBackButtonPressed();
			if (navigatedBack)
			{
				return Result.Ok();
			}
			else
			{
				const string error = "Back navigation got cancelled";
				_logger.LogError(error);
				return Result.Fail(ErrorCode.Cancelled, error);
			}
		}

		const string errorMessage = "Navigation back is only supported when MainPage is a NavigationPage or a Page";
		_logger.LogError(errorMessage);
		return Result.Fail(ErrorCode.NotSupported, errorMessage);
	}

	async Task<IResult> INavigationService.NavigateToRootAsync(INavigationParameters? parameters, bool animated)
	{
		if (TryGetCurrentPage(out var currentPage) == false)
		{
			return Result.Fail(ErrorCode.InvalidState, "Main page is not set for current window");
		}

		NavigationPage? navigationPage = currentPage switch
		{
			NavigationPage navPage => navPage,
			TabbedPage tabbedPage => tabbedPage.CurrentPage as NavigationPage,
			_ => null
		};

		if (navigationPage is null)
		{
			const string error = "Navigation to root is only supported when MainPage is a NavigationPage";
			_logger.LogError(error);
			return Result.Fail(ErrorCode.NotSupported, error);
		}

		if (navigationPage.Navigation.NavigationStack.Count <= 1)
		{
			const string error = "No page to navigate back to";
			_logger.LogError(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		await navigationPage.PopToRootAsync(animated);

		if (navigationPage.CurrentPage is Page rootPage)
		{
			if (rootPage.BindingContext is IRootPageAware rootPageAware)
			{
				rootPageAware.OnNavigatedToRoot(parameters ?? new NavigationParameters());
			}

			if (rootPage.BindingContext is IRootPageAwareAsync rootPageAwareAsync)
			{
				await rootPageAwareAsync.OnNavigatedToRootAsync(parameters ?? new NavigationParameters());
			}
		}

		return Result.Ok();
	}

	private async Task<IResult> HandleContextualNavigationAsync(Page? currentPage, IEnumerable<Page> newPages, bool animated)
	{
		if (currentPage is TabbedPage tabbedPage)
		{
			var currentTab = tabbedPage.CurrentPage;
			if (currentTab is null)
			{
				const string error = "No current tab found in the TabbedPage";
				_logger.LogError(error);
				return Result.Fail(ErrorCode.InvalidState, error);
			}

			if (currentTab is NavigationPage tabNavigationPage)
			{
				foreach (var page in newPages)
				{
					await tabNavigationPage.PushAsync(page, animated);
				}
			}
			else
			{
				const string error = "Relative navigation within a TabbedPage is only supported when the current tab is wrapped in NavigationPage";
				_logger.LogError(error);
				return Result.Fail(ErrorCode.NotSupported, error);
			}
		}
		else if (currentPage is NavigationPage navigationPage)
		{
			foreach (var page in newPages)
			{
				await navigationPage.PushAsync(page, animated);
			}
		}
		else
		{
			const string error = "Relative navigation is only supported when MainPage is a NavigationPage";
			_logger.LogError(error);
			return Result.Fail(ErrorCode.NotSupported, error);
		}

		return Result.Ok();
	}

	private static async Task PushPagesAsync(NavigationPage navigationPage, IEnumerable<Page> newPages, bool animated)
	{
		foreach (var page in newPages)
		{
			await navigationPage.PushAsync(page, animated);
		}
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

	private async void WindowEvent_Activated(object? sender, EventArgs e)
	{
		var currentApp = Application.Current;
		if (currentApp is null)
		{
			return;
		}

		Page? currentPage = null;

		if (currentApp.Windows[0].Page is Page page)
		{
			currentPage = page;
		}
		else if (currentApp.Windows[0].Page is NavigationPage navigationPage)
		{
			currentPage = navigationPage.CurrentPage;
		}

		if (TryGetViewModel(currentPage, out var viewModel))
		{
			if (viewModel is IWindowEventAware eventAware)
			{
				eventAware.OnWindowActivated();
			}

			if (viewModel is IWindowEventAwareAsync eventAwareAsync)
			{
				await eventAwareAsync.OnWindowActivatedAsync();
			}
		}
	}

	private async void Page_Appearing(object? sender, EventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is IAppearingAware appearingAware)
			{
				appearingAware.OnPageAppearing();
			}

			if (viewModel is IAppearingAwareAsync appearingAwareAsync)
			{
				await appearingAwareAsync.OnPageAppearingAsync();
			}
		}
	}

	private async void Page_Disappearing(object? sender, EventArgs e)
	{
		if (TryGetViewModel(sender, out var viewModel))
		{
			if (viewModel is IAppearingAware appearingAware)
			{
				appearingAware.OnPageDisappearing();
			}

			if (viewModel is IAppearingAwareAsync appearingAwareAsync)
			{
				await appearingAwareAsync.OnPageDisappearingAsync();
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

	private static bool TryGetCurrentPage([NotNullWhen(true)] out Page? page)
	{
		page = null;

		var currentApp = Application.Current;
		if (currentApp is null)
		{
			return false;
		}

		if (currentApp.Windows.Count > 0 && currentApp.Windows[0].Page is Page currentPage)
		{
			page = currentPage;
			return true;
		}

		return false;
	}

	// TODO implement page reuse strategy if needed.
	private Page CreatePage(PageInfo pageInfo, Type? viewModelType = null, INavigationParameters? parameters = null)
	{
		var page = (Page?)Activator.CreateInstance(pageInfo.PageType)
			?? throw new InvalidOperationException($"Could not create instance of page type '{pageInfo.PageType.FullName}'.");

		if (viewModelType is not null)
		{
			var viewModel = _serviceProvider.GetRequiredService(viewModelType);
			page.BindingContext = viewModel;

			if (viewModel is BaseViewModel baseViewModel)
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

		return page;
	}

	private PageInfo[] GetPageTypesFromPath(string path)
	{
		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		return [.. segments.Select(segment =>
		{
			var parts = segment.Split('?', StringSplitOptions.RemoveEmptyEntries);
			var pageName = parts[0];
			var queryParameters = parts.Length > 1 ? parts[1] : string.Empty;
			var queryDictionary = QueryStringHelper.ToDictionary(queryParameters);
			var pageType = FindPageTypeByName(pageName);
			return pageType == null
				? throw new InvalidOperationException($"Page '{pageName}' not found.")
				: new PageInfo(pageType, queryDictionary);
		})];
	}

	private Type? FindPageTypeByName(string pageName)
	{
		return GetAssemblyPageSourceTypes()
			.FirstOrDefault(t => t.Name.Equals(pageName, StringComparison.OrdinalIgnoreCase));
	}

	private Type[]? _cachedAssemblyPageSourceTypes;
	private Type[] GetAssemblyPageSourceTypes()
	{
		if (_cachedAssemblyPageSourceTypes is null)
		{
			var types = _assemblyPageSource?.GetTypes()
				.Where(t => typeof(Page).IsAssignableFrom(t)) ?? [];
			_cachedAssemblyPageSourceTypes = [.. types, typeof(NavigationPage)];
		}
		return _cachedAssemblyPageSourceTypes;
	}
}
