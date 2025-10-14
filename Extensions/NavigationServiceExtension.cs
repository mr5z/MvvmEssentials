using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;
using Nkraft.CrossUtility.Helpers;

namespace Nkraft.MvvmEssentials.Extensions;

public static class NavigationExtension
{
	internal static string ToPageName<TViewModel>()
	{
		const string KnownViewModelPattern = "ViewModel";
		const string KnownPagePattern = "Page";
		return typeof(TViewModel).Name.Replace(KnownViewModelPattern, KnownPagePattern);
	}

	/// <summary>
	/// Navigates to the page associated with the specified ViewModel type, passing parameters as a strongly-typed object.
	/// </summary>
	/// <typeparam name="TViewModel">The ViewModel type to navigate to. The corresponding Page must be registered.</typeparam>
	/// <typeparam name="TParameter">The type of the parameters object. Properties will be mapped to navigation parameters.</typeparam>
	/// <returns>An <see cref="IResult"/> indicating the outcome of the navigation operation.</returns>
	public static async Task<IResult> NavigateAsync<TViewModel, TParameter>(
		this INavigationService navigationService, TParameter? parameters = null, bool animated = true)
		where TViewModel : PageViewModel
		where TParameter : class
	{
		var pageName = ToPageName<TViewModel>();
		var dictionary = ObjectHelper.ToDictionary(parameters);
		INavigationParameters navParam = new NavigationParameters();
		foreach (var (key, value) in dictionary)
		{
			navParam.Add(key, value);
		}
		return await navigationService.NavigateAsync(pageName, navParam, animated);
	}

	/// <summary>
	/// Navigates to the page associated with the specified ViewModel type.
	/// </summary>
	/// <typeparam name="TViewModel">The ViewModel type to navigate to. The corresponding Page must be registered.</typeparam>
	/// <returns>An <see cref="IResult"/> indicating the outcome of the navigation operation.</returns>
	public static async Task<IResult> NavigateAsync<TViewModel>(
		this INavigationService navigationService, INavigationParameters? parameters = null, bool animated = true)
		where TViewModel : PageViewModel
	{
		var pageName = ToPageName<TViewModel>();
		return await navigationService.NavigateAsync(pageName, parameters, animated);
	}

	/// <summary>
	/// Marks the path with replacement indicator.
	/// </summary>
	/// <param name="withNavigation">If true, includes a <see cref="NavigationPage"/> in the path.</param>
	/// <returns>An <see cref="IPageLink"/> for building a navigation path.</returns>
	public static IPageLink Absolute(this INavigationService navigationService, bool withNavigation = true)
	{
		var rootPage = "/" + (withNavigation ? nameof(NavigationPage) : string.Empty);
		return new PageLink(navigationService, rootPage);
	}

	/// <summary>
	/// Starts the path with no indicators.
	/// </summary>
	/// <param name="withNavigation">If true, includes a <see cref="NavigationPage"/> in the path.</param>
	/// <returns>An <see cref="IPageLink"/> for building a navigation path.</returns>
	public static IPageLink Relative(this INavigationService navigationService, bool withNavigation = false)
	{
		var rootPage = withNavigation ? nameof(NavigationPage) : string.Empty;
		return new PageLink(navigationService, rootPage);
	}

	/// <summary>
	/// Appends a page segment to the navigation path for the specified ViewModel type.
	/// </summary>
	/// <typeparam name="TViewModel">The ViewModel type to append. The corresponding Page must be registered.</typeparam>
	/// <param name="parameters">If passed with an object, it must contain "primitive types" only.</param>
	/// <returns>An updated <see cref="IPageLink"/> with the new segment appended.</returns>
	public static IPageLink Push<TViewModel>(this IPageLink pageLink, object? parameters = null) where TViewModel : PageViewModel
	{
		return Push<TViewModel, object>(pageLink, parameters);
	}

	/// <summary>
	/// Appends a page segment to the navigation path for the specified ViewModel type, using a strongly-typed parameters object.
	/// </summary>
	/// <typeparam name="TViewModel">The ViewModel type to append. The corresponding Page must be registered.</typeparam>
	/// <typeparam name="TParameter">The type of the parameters object. Properties will be mapped to navigation parameters.</typeparam>
	/// <param name="parameters">If passed with an object, it must contain "primitive types" only.</param>
	/// <returns>An updated <see cref="IPageLink"/> with the new segment appended.</returns>
	public static IPageLink Push<TViewModel, TParameter>(this IPageLink pageLink, TParameter? parameters)
		where TViewModel : PageViewModel
		where TParameter : class
	{
		var page = ToPageName<TViewModel>();
		return pageLink.AppendSegment(page, typeof(TViewModel), parameters);
	}

	/// <summary>
	/// Navigates to the path aggregated from <see cref="IPageLink"/>.
	/// </summary>
	/// <param name="parameters">The last ViewModel in the <see cref="IPageLink"/> will only receive this.</param>
	/// <returns>An <see cref="IResult"/> indicating the outcome of the navigation operation.</returns>
	public static async Task<IResult> NavigateAsync(this IPageLink pageLink,
		INavigationParameters? parameters = null, bool animated = true)
	{
		var navigationService = ((PageLink)pageLink).NavigationService;
		var fullPath = pageLink.FullPath;
		return await navigationService.NavigateAsync(fullPath, parameters, animated);
	}
}