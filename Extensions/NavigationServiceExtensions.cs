using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;
using Nkraft.CrossUtility.Helpers;

namespace Nkraft.MvvmEssentials.Extensions;

internal static class NavigationExtension
{
	private const string KnownViewModelPattern = "ViewModel";
	private const string KnownPagePattern = "Page";

	internal static string ToPageName<TViewModel>() where TViewModel : PageViewModel
	{
		return typeof(TViewModel).Name.Replace(KnownViewModelPattern, KnownPagePattern);
	}

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

	public static async Task<IResult> NavigateAsync<TViewModel>(
		this INavigationService navigationService, INavigationParameters? parameters = null, bool animated = true)
		where TViewModel : PageViewModel
	{
		var pageName = ToPageName<TViewModel>();
		return await navigationService.NavigateAsync(pageName, parameters, animated);
	}

	public static IPageLink Absolute(this INavigationService navigationService, bool withNavigation = true)
	{
		var rootPage = "/" + (withNavigation ? nameof(NavigationPage) : string.Empty);
		return new PageLink(navigationService, rootPage);
	}

	public static IPageLink Relative(this INavigationService navigationService, bool withNavigation = false)
	{
		var rootPage = withNavigation ? nameof(NavigationPage) : string.Empty;
		return new PageLink(navigationService, rootPage);
	}

	public static IPageLink Push<TViewModel>(this IPageLink pageLink, object? parameters = null) where TViewModel : PageViewModel
	{
		return Push<TViewModel, object>(pageLink, parameters);
	}

	public static IPageLink Push<TViewModel, TParameter>(this IPageLink pageLink, TParameter? parameters)
		where TViewModel : PageViewModel
		where TParameter : class
	{
		var page = ToPageName<TViewModel>();
		return pageLink.AppendSegment(page, typeof(TViewModel), parameters);
	}

	public static async Task<IResult> NavigateAsync(this IPageLink pageLink,
		INavigationParameters? parameters = null, bool animated = true)
	{
		var navigationService = (pageLink as PageLink)!.NavigationService;
		var fullPath = pageLink.FullPath;
		return await navigationService.NavigateAsync(fullPath, parameters, animated);
	}
}