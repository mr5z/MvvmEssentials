using Microsoft.Extensions.Logging;
using Nkraft.CrossUtility.Patterns;
using Nkraft.Mopups.Contracts;
using Nkraft.MvvmEssentials.Services.Navigation;
using Nkraft.MvvmEssentials.Services.Pages;
using Nkraft.MvvmEssentials.Services.Presentations;
using Nkraft.MvvmEssentials.ViewModels;
using PopupPage = Nkraft.Mopups.Pages.PopupPage;

namespace Nkraft.MvvmEssentials.Services;

public interface IPopupService
{
	Task<IResult> PresentAsync(string popupName, INavigationParameters? parameters = null, bool animated = false);

	Task<IResult> DismissAsync(string? popupName = null, bool animated = true);

	Task<IResult> DismissAllAsync(bool animated = true);
}

internal sealed class PopupService : IPopupService, IPresenter
{
	private readonly ILogger<PopupService> _logger;
	private readonly IPopupNavigation _popupNavigation;
	private readonly IPageFactory _pageFactory;
	private readonly IPageCatalog _pageCatalog;

	private readonly OrderedDictionary<Type, WeakReference<PopupPage>> _activePopups = [];

	// The popups this service presented and that are still on screen
	private readonly Dictionary<PopupPage, Presentation> _presentations = [];

	public PopupService(
		ILogger<PopupService> logger, 
		IPopupNavigation popupNavigation,
		IPageFactory pageFactory,
		IPageCatalog pageCatalog)
	{
		_logger = logger;
		_popupNavigation = popupNavigation;
		_pageFactory = pageFactory;
		_pageCatalog = pageCatalog;

		_pageFactory.PageUnloaded += PageFactory_PageUnloaded;
	}

	private void PageFactory_PageUnloaded(object? sender, Page page)
	{
		if (page is PopupPage popup)
		{
			// Mopups removed it (ours, a background tap, PopAllAsync): complete a waiting caller
			if (_presentations.Remove(popup, out var presentation))
			{
				presentation.OnClosed();
			}

			var viewModel = popup.BindingContext as PageViewModel;
			var popupRemoved = _activePopups.Remove(popup.GetType());
			if (popupRemoved == false)
			{
				var popupName = viewModel?.PageName ?? popup.GetType().Name;
				_logger.LogWarning("Popup '{PopupName}' was unloaded but not found in active popups.", popupName);
			}
		}
	}

	async Task<IResult> IPopupService.PresentAsync(string popupName, INavigationParameters? parameters, bool animated)
	{
		PageInfo[] pageInfoList;

		try
		{
			pageInfoList = _pageFactory.GetPageTypesFromPath(popupName);
		}
		catch (Exception ex)
		{
			const string message = "An error occurred when trying to decode popup name '{PopupName}'.";
			_logger.LogError(ex, message, popupName);
			return Result.Fail(ErrorCode.General, ex, message);
		}

		if (pageInfoList.Length > 1)
		{
			const string message = "More than one popup found with name '{PopupName}'.";
			_logger.LogWarning(message, popupName);
			return Result.Fail(ErrorCode.InvalidParameter, message, popupName);
		}

		using var lease = new PageLease(_pageFactory, _logger);

		try
		{
			// PresentAsync<TViewModel, TResult> passes a presentation that the caller awaits; otherwise nobody does
			var popupParameters = parameters ?? new NavigationParameters();
			if (popupParameters.TryGetValue<Presentation>(NavigationHints.Presentation, out var presentation) == false)
			{
				presentation = new Presentation();
				popupParameters[NavigationHints.Presentation] = presentation;
			}

			var pageInfo = pageInfoList.Single();
			var popupPage = (PopupPage)lease.Create(pageInfo, popupParameters);
			var popupType = popupPage.GetType();
			presentation.Open(this, popupPage, popupPage);

			await _popupNavigation.PushAsync(popupPage, animated);
			lease.Commit(popupPage);
			_activePopups[popupType] = new WeakReference<PopupPage>(popupPage);
			_presentations[popupPage] = presentation;
		}
		catch (Exception ex)
		{
			const string message = "An error occurred while trying to show '{PopupName}'.";
			_logger.LogError(ex, message, popupName);
			return Result.Fail(ErrorCode.General, ex, message, popupName);
		}
		
		return Result.Ok();
	}

	async Task<IResult> IPopupService.DismissAsync(string? popupName, bool animated)
	{
		try
		{
			if (string.IsNullOrEmpty(popupName))
			{
				await _popupNavigation.PopAsync(animated);
				_logger.LogInformation("No popup name specified. Dismissing current active popup.");
				// No need to manually remove from _activePopups here, as it will be handled in PageFactory_PageUnloaded.
				return Result.Ok();
			}
			
			var popupType = _pageCatalog.ResolvePageType(popupName);
			if (popupType is null)
			{
				const string message = "Unable to resolve popup '{PopupName}' from registry.";
				_logger.LogWarning(message, popupName);
				return Result.Fail(ErrorCode.General, message, popupName);
			}

			if (_activePopups.TryGetValue(popupType, out var popupRef) == false)
			{
				const string message = "No active popup found with name '{PopupName}'.";
				_logger.LogWarning(message, popupName);
				return Result.Fail(ErrorCode.InvalidState, message, popupName);
			}

			if (popupRef.TryGetTarget(out var popupPage) == false)
			{
				const string message = "Referenced popup '{PopupName}' could not be recovered.";
				_logger.LogWarning(message, popupName);
				return Result.Fail(ErrorCode.InvalidState, message, popupName);
			}
			
			await _popupNavigation.RemovePageAsync(popupPage, animated);
		}
		catch (Exception ex)
		{
			const string message = "An error occurred while trying to dismiss popup '{PopupName}'.";
			_logger.LogError(ex, message, popupName);
			return Result.Fail(ErrorCode.General, ex, message, popupName);
		}
		
		return Result.Ok();
	}

	async Task<IResult> IPopupService.DismissAllAsync(bool animated)
	{
		try
		{
			await _popupNavigation.PopAllAsync(animated);
			// TODO figure out if this is really needed
			_activePopups.Clear();
			CloseAllPresentations();
		}
		catch (Exception ex)
		{
			const string message = "An error occurred while trying to dismiss all popups.";
			_logger.LogError(ex, message);
			return Result.Fail(ErrorCode.General, ex, message);
		}
		
		return Result.Ok();
	}

	// -----------------------------------------------------------------------
	// IPresenter: what a presentation asks for
	// -----------------------------------------------------------------------

	async Task<IResult> IPresenter.DismissAsync(Presentation presentation)
	{
		if (presentation.IsClosed || presentation.Host is not PopupPage popupPage)
		{
			const string message = "The popup has already closed.";
			_logger.LogInformation(message);
			return Result.Fail(ErrorCode.NotHandled, message);
		}

		try
		{
			// Removes this popup wherever it is in the popup stack; PageUnloaded then closes the presentation
			await _popupNavigation.RemovePageAsync(popupPage, animate: true);
		}
		catch (Exception ex)
		{
			const string message = "An error occurred while trying to dismiss popup '{PopupName}'.";
			_logger.LogError(ex, message, popupPage.GetType().Name);
			return Result.Fail(ErrorCode.General, ex, message, popupPage.GetType().Name);
		}
		
		return Result.Ok();
	}

	private void CloseAllPresentations()
	{
		var closed = _presentations.Values.ToList();
		_presentations.Clear();
		closed.ForEach(presentation => presentation.OnClosed());
	}
}
