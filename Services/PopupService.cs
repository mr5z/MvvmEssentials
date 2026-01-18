using Microsoft.Extensions.Logging;
using Mopups.Interfaces;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.ViewModels;
using PopupPage = Mopups.Pages.PopupPage;

namespace Nkraft.MvvmEssentials.Services;

public interface IPopupService
{
	Task<IResult> PresentAsync(string popupName, INavigationParameters? parameters = null, bool animated = false);

	Task<IResult> DismissAsync(string? popupName = null, bool animated = true);

	Task<IResult> DismissAllAsync(bool animated = true);
}

internal sealed class PopupService : IPopupService
{
	private readonly ILogger<PopupService> _logger;
	private readonly IPopupNavigation _popupNavigation;
	private readonly IPageFactory _pageFactory;

	private readonly OrderedDictionary<string, WeakReference<PopupPage>> _activePopups = [];

	public PopupService(ILogger<PopupService> logger, IPopupNavigation popupNavigation, IPageFactory pageFactory)
	{
		_logger = logger;
		_popupNavigation = popupNavigation;
		_pageFactory = pageFactory;

		_pageFactory.PageUnloaded += PageFactory_PageUnloaded;
	}

	private void PageFactory_PageUnloaded(object? sender, Page page)
	{
		if (page is PopupPage popup)
		{
			var viewModel = popup.BindingContext as BaseViewModel;
			var popupKey = viewModel?.PageName ?? popup.GetType().Name;
			var popupRemoved = _activePopups.Remove(popupKey);
			if (popupRemoved == false)
			{
				_logger.LogWarning("Popup '{PopupName}' was unloaded but not found in active popups.", popupKey);
			}
		}
	}

	async Task<IResult> IPopupService.PresentAsync(string popupName, INavigationParameters? parameters, bool animated)
	{
		PageInfo[] pageInfoList;

		try
		{
			pageInfoList = _pageFactory.GetPageTypesFromPath<PopupPage>(popupName);
		}
		catch (Exception ex)
		{
			const string error = "An error occurred when trying to decode popup name '{PopupName}'.";
			_logger.LogError(ex, error, popupName);
			return Result.Fail(ErrorCode.General, error);
		}

		if (pageInfoList.Length > 1)
		{
			const string error = "More than one popup found with a name '{PopupName}'.";
			_logger.LogWarning(error, popupName);
			return Result.Fail(ErrorCode.General, error, popupName);
		}

		try
		{
			var pageInfo = pageInfoList.Single();
			var popupPage = (PopupPage)_pageFactory.CreatePage(pageInfo, parameters);
			await _popupNavigation.PushAsync(popupPage, animated);
			_activePopups[popupName] = new WeakReference<PopupPage>(popupPage);
			return Result.Ok();
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to show '{PopupName}'.";
			_logger.LogError(ex, error, popupName);
			return Result.Fail(ErrorCode.General, error, popupName);
		}
	}

	async Task<IResult> IPopupService.DismissAsync(string? popupName, bool animated)
	{
		try
		{
			if (string.IsNullOrEmpty(popupName))
			{
				await _popupNavigation.PopAsync(animated);
				// No need to manually remove from _activePopups here, as it will be handled in PageFactory_PageUnloaded.
				return Result.Ok();
			}
			else
			{
				if (_activePopups.TryGetValue(popupName, out var popupRef) && popupRef.TryGetTarget(out var popupPage))
				{
					await _popupNavigation.RemovePageAsync(popupPage, animated);
					// Ditto regarding removal from _activePopups.
					return Result.Ok();
				}
				else
				{
					const string error = "Failed dismiss popup '{PopupName}'.";
					_logger.LogWarning(error, popupName);
					return Result.Fail(ErrorCode.InvalidState, error, popupName);
				}
			}
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to dismiss popup '{PopupName}'.";
			_logger.LogError(ex, error, popupName);
			return Result.Fail(ErrorCode.General, error, popupName);
		}
	}

	async Task<IResult> IPopupService.DismissAllAsync(bool animated)
	{
		try
		{
			await _popupNavigation.PopAllAsync(animated);
			return Result.Ok();
		}
		catch (Exception ex)
		{
			const string error = "An error occurred while trying to dismiss all popups.";
			_logger.LogError(ex, error);
			return Result.Fail(ErrorCode.General, error);
		}
	}
}
