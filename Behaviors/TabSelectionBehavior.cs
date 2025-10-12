using Nkraft.MvvmEssentials.Services.Navigation;
using System.ComponentModel;

namespace Nkraft.MvvmEssentials.Behaviors;

public class TabSelectionBehavior : Behavior<TabbedPage>
{
	private int _previousTabIndex = -1;

	protected override void OnAttachedTo(TabbedPage bindable)
	{
		base.OnAttachedTo(bindable);
		bindable.BindingContextChanged += TabbedPage_BindingContextChanged;
		bindable.CurrentPageChanged += TabbedPage_CurrentPageChanged;
	}

	protected override void OnDetachingFrom(TabbedPage bindable)
	{
		base.OnDetachingFrom(bindable);
		bindable.BindingContextChanged -= TabbedPage_BindingContextChanged;
		bindable.CurrentPageChanged -= TabbedPage_CurrentPageChanged;
		if (bindable?.BindingContext is INotifyPropertyChanged notifiableObject)
		{
			notifiableObject.PropertyChanged -= TabbedPage_BindingContextPropertyChanged;
		}
	}

	private void TabbedPage_CurrentPageChanged(object? sender, EventArgs e)
	{
		if (sender is not TabbedPage tabbedPage)
		{
			return;
		}

		var tabIndex = tabbedPage.Children.IndexOf(tabbedPage.CurrentPage);
		if (tabIndex == -1)
		{
			return;
		}

		if (tabbedPage.BindingContext is ITabHost tabHost)
		{
			tabHost.SelectedTabIndex = tabIndex;

			if (_previousTabIndex != -1)
			{
				var previousTab = tabHost.GetTabs().ElementAt(_previousTabIndex);
				previousTab.OnTabUnselected();
			}

			tabHost.CurrentTab.OnTabSelected();
			_previousTabIndex = tabIndex;
		}
	}

	private void TabbedPage_BindingContextChanged(object? sender, EventArgs e)
	{
		if (sender is not BindableObject bindableObject)
		{
			return;
		}

		if (bindableObject.BindingContext is INotifyPropertyChanged notifiableObject)
		{
			notifiableObject.PropertyChanged -= TabbedPage_BindingContextPropertyChanged;
			notifiableObject.PropertyChanged += TabbedPage_BindingContextPropertyChanged;
		}
	}

	private void TabbedPage_BindingContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ITabHost.SelectedTabIndex))
		{
			return;
		}

		if (sender is not TabbedPage tabbedPage)
		{
			return;
		}

		if (tabbedPage.BindingContext is not ITabHost tab)
		{
			return;
		}

		var tabIndex = tab.SelectedTabIndex;
		var destinationPage = tabbedPage.Children.ElementAtOrDefault(tabIndex);
		if (destinationPage is not null)
		{
			tabbedPage.CurrentPage = destinationPage;
		}
	}
}
