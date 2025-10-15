using Microsoft.Extensions.Logging;
using Mopups.Interfaces;
using Nkraft.CrossUtility.Patterns;
using Nkraft.MvvmEssentials.Services.Navigation;
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

	private readonly Dictionary<string, WeakReference<PopupPage>> _activePopups = [];

	public PopupService(ILogger<PopupService> logger, IPopupNavigation popupNavigation, IPageFactory pageFactory)
	{
		_logger = logger;
		_popupNavigation = popupNavigation;
		_pageFactory = pageFactory;

		_pageFactory.PageUnloaded += PageFactory_PageUnloaded;
	}

	internal bool RemovePopupDueToCancellation(string popupName)
	{
		return _activePopups.Remove(popupName);
	}

	private void PageFactory_PageUnloaded(object? sender, Page page)
	{
		if (page is PopupPage popup)
		{
			var popupKey = popup.GetType().Name;
			_activePopups.Remove(popupKey);
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
			_logger.LogError(error, popupName);
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
				return Result.Ok();
			}
			else
			{
				if (_activePopups.TryGetValue(popupName, out var popupRef) && popupRef.TryGetTarget(out var popupPage))
				{
					await _popupNavigation.RemovePageAsync(popupPage, animated);
					_activePopups.Remove(popupName);
					return Result.Ok();
				}
				else
				{
					const string error = "Failed dismiss popup '{PopupName}'.";
					_logger.LogError(error, popupName);
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
