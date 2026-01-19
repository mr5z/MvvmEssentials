using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Extensions;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;

namespace Nkraft.MvvmEssentials.Services;

/// <summary>
/// Defines navigation operations for navigating between pages in a .NET MAUI application.
/// </summary>
public interface INavigationService
{
	/// <summary>
	/// Navigates to the sequence of pages defined by the specified <paramref name="path"/>.
	/// </summary>
	/// <remarks>
	/// <list type="bullet">
	///   <item>
	///     <description>
	///     Page names in the path must correspond to page types that exist in the configured assembly
	///     (see <c>NavigationOptions.AssemblyPageSource</c>).
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///     Prefix the path with <c>//</c> to perform absolute navigation, which replaces the application's
	///     main page and clears the navigation stack.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///     Include <c>NavigationPage</c> in the path to wrap subsequent pages (separated by '/') inside
	///     a <see cref="NavigationPage"/>.
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///     Query parameters may be appended to page names using <c>?</c>
	///     (for example, <c>MyPage?param1=value1</c>).
	///     </description>
	///   </item>
	///   <item>
	///     <description>
	///     Relative navigation (paths without <c>//</c>) pushes pages onto the current navigation stack,
	///     or onto the active tab's stack when hosted in a <see cref="TabbedPage"/>.
	///     </description>
	///   </item>
	/// </list>
	/// </remarks>
	/// <param name="path">
	/// Navigation path (for example, <c>//MainPage/NavigationPage/DetailsPage</c>).
	/// </param>
	/// <param name="parameters">
	/// Optional parameters passed to the target page's view model.
	/// </param>
	/// <param name="animated">
	/// Indicates whether the navigation transition should be animated.
	/// </param>
	/// <returns>
	/// An <see cref="IResult"/> indicating whether the navigation operation succeeded.
	/// </returns>
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
	private readonly IPageFactory _pageFactory;

	public NavigationService(
		ILogger<NavigationService> logger,
		IWindowEventHandler windowHandler,
		IPageFactory pageFactory)
	{
		_logger = logger;
		_pageFactory = pageFactory;

		windowHandler.Activated += WindowEvent_Activated;
	}

	// Handle support for FlyoutPage, and nested NavigationPage if needed in the future.
	async Task<IResult> INavigationService.NavigateAsync(string path, INavigationParameters? parameters, bool animated)
	{
		var currentApp = Application.Current;
		if (currentApp is null)
		{
			const string error = "Application.Current is null.";
			_logger.LogWarning(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		PageInfo[] pageInfoList;

		try
		{
			pageInfoList = _pageFactory.GetPageTypesFromPath<Page>(path);
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to fetch page information.";
			_logger.LogError(ex, error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		if (pageInfoList.Length == 0)
		{
			const string error = "No valid pages found in the navigation path.";
			_logger.LogWarning(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		try
		{
			// Build the page stack
			var pages = pageInfoList.Select(pageInfo => _pageFactory.CreatePage(pageInfo, parameters)).ToArray();
			var replaceCurrentPage = path.StartsWith('/');

			if (replaceCurrentPage)
			{
				if (pages.Length == 0)
				{
					const string error = "No valid pages to navigate to.";
					_logger.LogWarning(error);
					return Result.Fail(ErrorCode.InvalidState, error);
				}

				var firstPage = pages.First();
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
						const string error = "Absolute navigation within a TabbedPage is only supported when the current tab is wrapped in NavigationPage.";
						_logger.LogWarning(error);
						return Result.Fail(ErrorCode.NotSupported, error);
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
					const string error = "Current page is null.";
					_logger.LogWarning(error);
					return Result.Fail(ErrorCode.InvalidState, error);
				}
				return await HandleContextualNavigationAsync(currentPage, pages, animated);
			}
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to perform page navigation (Path: {Path}).";
			_logger.LogError(ex, error, path);
			return Result.Fail(ErrorCode.Unknown, error);
		}

		return Result.Ok();
	}

	async Task<IResult> INavigationService.NavigateBackAsync(bool animated)
	{
		if (TryGetCurrentPage(out var currentPage) == false)
		{
			const string error = "Main page is not set for current window.";
			_logger.LogWarning(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		if (currentPage is NavigationPage navigationPage)
		{
			var isRootPage = navigationPage.Navigation.NavigationStack.Count == 0;
			if (isRootPage)
			{
				const string error = "No page to navigate back to.";
				_logger.LogWarning(error);
				return Result.Fail(ErrorCode.InvalidState, error);
			}

			var previousPage = await navigationPage.PopAsync(animated);
			if (previousPage is null)
			{
				const string error = "Popped page returns null.";
				_logger.LogWarning(error);
				return Result.Fail(ErrorCode.InvalidState, error);
			}
			
			return Result.Ok();
		}
		
		var navigatedBack = currentPage.SendBackButtonPressed();
		if (navigatedBack)
		{
			return Result.Ok();
		}
		
		const string errorMessage = "Back navigation got cancelled.";
		_logger.LogWarning(errorMessage);
		return Result.Fail(ErrorCode.Cancelled, errorMessage);
	}

	async Task<IResult> INavigationService.NavigateToRootAsync(INavigationParameters? parameters, bool animated)
	{
		if (TryGetCurrentPage(out var currentPage) == false)
		{
			const string error = "Main page is not set for current window.";
			_logger.LogWarning(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		NavigationPage? navigationPage = currentPage switch
		{
			NavigationPage navPage => navPage,
			TabbedPage tabbedPage => tabbedPage.CurrentPage as NavigationPage,
			_ => null
		};

		if (navigationPage is null)
		{
			const string error = "Root navigation is only supported within NavigationPage.";
			_logger.LogWarning(error);
			return Result.Fail(ErrorCode.NotSupported, error);
		}

		if (navigationPage.Navigation.NavigationStack.Count <= 1)
		{
			const string error = "No page to navigate back to.";
			_logger.LogWarning(error);
			return Result.Fail(ErrorCode.InvalidState, error);
		}

		try
		{
			await navigationPage.PopToRootAsync(animated);
			var rootPage = navigationPage.CurrentPage;
			if (rootPage.BindingContext is IRootPageAware rootPageAware)
			{
				rootPageAware.OnNavigatedToRoot(parameters ?? new NavigationParameters());
			}

			if (rootPage.BindingContext is IRootPageAwareAsync rootPageAwareAsync)
			{
				await rootPageAwareAsync.OnNavigatedToRootAsync(parameters ?? new NavigationParameters());
			}
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to navigate to root.";
			_logger.LogError(ex, error);
			return Result.Fail(ErrorCode.General, error);
		}

		return Result.Ok();
	}

	private async Task<IResult> HandleContextualNavigationAsync(Page? currentPage, IEnumerable<Page> newPages, bool animated)
	{
		switch (currentPage)
		{
			case TabbedPage tabbedPage:
			{
				var currentTab = tabbedPage.CurrentPage;
				switch (currentTab)
				{
					case NavigationPage tabNavigationPage:
					{
						foreach (var page in newPages)
						{
							await tabNavigationPage.PushAsync(page, animated);
						}

						break;
					}
					case null:
					{
						const string error = "No current tab found in the TabbedPage.";
						_logger.LogWarning(error);
						return Result.Fail(ErrorCode.InvalidState, error);
					}
					default:
					{
						const string error = "Relative navigation within a TabbedPage is only supported when the current tab is wrapped in a NavigationPage.";
						_logger.LogWarning(error);
						return Result.Fail(ErrorCode.NotSupported, error);
					}
				}

				break;
			}
			case NavigationPage navigationPage:
			{
				foreach (var page in newPages)
				{
					await navigationPage.PushAsync(page, animated);
				}

				break;
			}
			default:
			{
				const string error = "Relative navigation is only supported when root page is a NavigationPage.";
				_logger.LogWarning(error);
				return Result.Fail(ErrorCode.NotSupported, error);
			}
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

	private void WindowEvent_Activated(object? sender, EventArgs e)
	{
		var currentApp = Application.Current;
		if (currentApp is null)
		{
			_logger.LogWarning("Application.Current null detected at '{MethodName}'.", nameof(WindowEvent_Activated));
			return;
		}

		Page? currentPage = null;

		if (currentApp.Windows[0].Page is { } page)
		{
			currentPage = page;
		}
		else if (currentApp.Windows[0].Page is NavigationPage navigationPage)
		{
			currentPage = navigationPage.CurrentPage;
		}

		if (currentPage?.BindingContext is { } viewModel)
		{
			if (viewModel is IWindowEventAware eventAware)
			{
				eventAware.OnWindowActivated();
			}

			if (viewModel is IWindowEventAwareAsync eventAwareAsync)
			{
				eventAwareAsync.OnWindowActivatedAsync().FireAndForget(exception =>
				{
					_logger.LogWarning(exception, 
						"An error occurred while trying to invoke {MethodName}.", 
						nameof(eventAwareAsync.OnWindowActivatedAsync)
					);
				});
			}
		}
	}

	private static bool TryGetCurrentPage([NotNullWhen(true)] out Page? page)
	{
		var currentApp = Application.Current;
		if (currentApp is null)
		{
			page = null;
			return false;
		}

		if (currentApp.Windows.Count > 0 && currentApp.Windows[0].Page is { } currentPage)
		{
			page = currentPage;
			return true;
		}

		page = null;
		return false;
	}
}
