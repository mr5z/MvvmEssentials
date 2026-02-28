using Nkraft.CrossUtility.Helpers;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Helpers;
using Nkraft.MvvmEssentials.Services;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;

namespace Nkraft.MvvmEssentials.Extensions;

public static class NavigationExtension
{
	extension(INavigationService navigationService)
	{
		/// <summary>
		/// Navigates to the page associated with the specified ViewModel type, passing parameters as a strongly-typed object.
		/// </summary>
		/// <typeparam name="TViewModel">The ViewModel type to navigate to. The corresponding Page must be registered.</typeparam>
		/// <typeparam name="TParameter">The type of the parameters object. Properties will be mapped to navigation parameters.</typeparam>
		/// <returns>An <see cref="IResult"/> indicating the outcome of the navigation operation.</returns>
		public async Task<IResult> NavigateAsync<TViewModel, TParameter>(TParameter? parameters = null, bool animated = true)
			where TViewModel : PageViewModel
			where TParameter : class
		{
			var pageName = PageHelper.ToPageName<TViewModel>("Page");
			var dictionary = ObjectHelper.ToDictionary(parameters);
			var navParam = new NavigationParameters();
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
		public async Task<IResult> NavigateAsync<TViewModel>(INavigationParameters? parameters = null, bool animated = true)
			where TViewModel : PageViewModel
		{
			var pageName = PageHelper.ToPageName<TViewModel>("Page");
			return await navigationService.NavigateAsync(pageName, parameters, animated);
		}

		/// <summary>
		/// Marks the path with replacement indicator.
		/// </summary>
		/// <param name="withNavigation">If true, includes a <see cref="NavigationPage"/> in the path.</param>
		/// <returns>An <see cref="IPageLink"/> for building a navigation path.</returns>
		public IPageLink Absolute(bool withNavigation = true)
		{
			var rootPage = "/" + (withNavigation ? nameof(NavigationPage) : string.Empty);
			return new PageLink(navigationService, rootPage);
		}

		/// <summary>
		/// Starts the path with no indicators.
		/// </summary>
		/// <param name="withNavigation">If true, includes a <see cref="NavigationPage"/> in the path.</param>
		/// <returns>An <see cref="IPageLink"/> for building a navigation path.</returns>
		public IPageLink Relative(bool withNavigation = false)
		{
			var rootPage = withNavigation ? nameof(NavigationPage) : string.Empty;
			return new PageLink(navigationService, rootPage);
		}
	}

	extension(IPageLink pageLink)
	{
		/// <summary>
		/// Appends a page segment to the navigation path for the specified ViewModel type, using a strongly-typed parameters object.
		/// </summary>
		/// <typeparam name="TViewModel">The ViewModel type to append. The corresponding Page must be registered.</typeparam>
		/// <typeparam name="TParameter">The type of the parameters object. Properties will be mapped to navigation parameters.</typeparam>
		/// <param name="parameters">If passed with an object, it must contain "primitive types" only.</param>
		/// <returns>An updated <see cref="IPageLink"/> with the new segment appended.</returns>
		public IPageLink Push<TViewModel, TParameter>(TParameter? parameters)
			where TViewModel : PageViewModel
			where TParameter : class
		{
			var pageName = PageHelper.ToPageName<TViewModel>("Page");
			return pageLink.AppendSegment(pageName, typeof(TViewModel), parameters);
		}

		/// <summary>
		/// Navigates to the path aggregated from <see cref="IPageLink"/>.
		/// </summary>
		/// <param name="parameters">The last ViewModel in the <see cref="IPageLink"/> will only receive this.</param>
		/// <returns>An <see cref="IResult"/> indicating the outcome of the navigation operation.</returns>
		public async Task<IResult> NavigateAsync(INavigationParameters? parameters = null, bool animated = true)
		{
			var navigationService = ((PageLink)pageLink).NavigationService;
			var fullPath = pageLink.FullPath;
			return await navigationService.NavigateAsync(fullPath, parameters, animated);
		}

		/// <summary>
		/// Appends a page segment to the navigation path for the specified ViewModel type.
		/// </summary>
		/// <typeparam name="TViewModel">The ViewModel type to append. The corresponding Page must be registered.</typeparam>
		/// <param name="parameters">If passed with an object, it must contain "primitive types" only.</param>
		/// <returns>An updated <see cref="IPageLink"/> with the new segment appended.</returns>
		public IPageLink Push<TViewModel>(object? parameters = null) where TViewModel : PageViewModel
		{
			return pageLink.Push<TViewModel, object>(parameters);
		}
	}
}