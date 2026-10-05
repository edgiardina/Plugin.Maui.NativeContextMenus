using System.Collections.Specialized;
using System.ComponentModel;
using CoreFoundation;
using CoreGraphics;
using UIKit;

namespace Plugin.Maui.NativeContextMenus;

public partial class NativeContextMenuToolbarItem
{
    // The retries stop after approximately 3 seconds
    const int MaxAttachRetries = 6;
    const int FirstAttachRetryMilliseconds = 50;

    static int lastAutomationId;

    Page? hookedPage;
    int lastSchedule;
    NativeContextMenus.MenuButton? fallbackButton;

    // .NET MAUI makes a new UIBarButtonItem for this item each time it updates the navigation bar.
    // Thus the menu is attached again after each event that can cause an update.
    partial void PlatformParentSet()
    {
        // .NET MAUI copies AutomationId to the bar button. This is how the bar button is found.
        if (Parent is not null && string.IsNullOrEmpty(AutomationId))
            AutomationId = $"NativeContextMenuToolbarItem{Interlocked.Increment(ref lastAutomationId)}";

        if (hookedPage is not null)
        {
            hookedPage.Loaded -= OnPageChanged;
            hookedPage.Appearing -= OnPageChanged;
            hookedPage.PropertyChanged -= OnPagePropertyChanged;
            ((INotifyCollectionChanged)hookedPage.ToolbarItems).CollectionChanged -= OnToolbarItemsChanged;
            if (hookedPage is Shell oldShell)
                oldShell.Navigated -= OnShellNavigated;
        }

        hookedPage = Parent as Page;

        if (hookedPage is not null)
        {
            hookedPage.Loaded += OnPageChanged;
            hookedPage.Appearing += OnPageChanged;
            hookedPage.PropertyChanged += OnPagePropertyChanged;
            ((INotifyCollectionChanged)hookedPage.ToolbarItems).CollectionChanged += OnToolbarItemsChanged;
            if (hookedPage is Shell shell)
                shell.Navigated += OnShellNavigated;

            ScheduleAttach();
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == TextProperty.PropertyName ||
            propertyName == IconImageSourceProperty.PropertyName ||
            propertyName == IsEnabledProperty.PropertyName)
            ScheduleAttach();
    }

    void OnPageChanged(object? sender, EventArgs e) => ScheduleAttach();

    void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e) => ScheduleAttach();

    void OnToolbarItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScheduleAttach();

    void OnShellNavigated(object? sender, ShellNavigatedEventArgs e) => ScheduleAttach();

    // .NET MAUI does some updates in a main thread callback. Two callbacks put the attach after them.
    void ScheduleAttach()
    {
        var schedule = ++lastSchedule;
        MainThread.BeginInvokeOnMainThread(() => MainThread.BeginInvokeOnMainThread(() => TryAttach(schedule, 0)));
    }

    // .NET MAUI can make the bar button some time after the last event, for example after a tab change.
    // Thus the attach is tried again, with a longer delay each time, until the bar button is there.
    void TryAttach(int schedule, int attempt)
    {
        if (schedule != lastSchedule || Attach() || attempt >= MaxAttachRetries)
            return;

        var delay = TimeSpan.FromMilliseconds(FirstAttachRetryMilliseconds << attempt);
        DispatchQueue.MainQueue.DispatchAfter(new DispatchTime(DispatchTime.Now, delay), () => TryAttach(schedule, attempt + 1));
    }

    // Returns true when the bar button of this item has the menu
    bool Attach()
    {
        var attached = false;
        var controller = (FindPage()?.Handler as IPlatformViewHandler)?.ViewController;

        // A NavigationPage puts the bar buttons on a parent of the page view controller
        for (; controller is not null and not UINavigationController; controller = controller.ParentViewController)
        {
            foreach (var barButton in controller.NavigationItem.RightBarButtonItems ?? [])
            {
                if (barButton.AccessibilityIdentifier != AutomationId)
                    continue;

                attached = true;
                if (barButton.Menu is not null)
                    continue;

                // A bar button with an action shows its menu only on a long press
                barButton.Target = null;
                barButton.Action = null;
                barButton.Menu = CreateMenu();
            }
        }

        return attached;
    }

    // The elements are built each time the menu opens, so the menu always shows the current node state
    UIMenu CreateMenu()
    {
        var weakItem = new WeakReference<NativeContextMenuToolbarItem>(this);

        var deferred = UIDeferredMenuElement.CreateUncached(completion => completion(
            weakItem.TryGetTarget(out var item)
                ? NativeContextMenus.BuildElements(item, item.FindPage()?.Handler?.MauiContext)
                : []));

        return UIMenu.Create(string.Empty, [deferred]);
    }

    // The bar button calls OnClicked only when it has no menu, which occurs when an update of the
    // navigation bar was not seen. The menu is attached for the next tap and opens now from a clear button.
    partial void PlatformClicked()
    {
        Attach();

        var controller = (FindPage()?.Handler as IPlatformViewHandler)?.ViewController;
        if (controller?.NavigationController?.NavigationBar is not { } bar)
            return;

        const int size = 44;
        var isRightToLeft = bar.EffectiveUserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft;

        fallbackButton?.RemoveFromSuperview();
        fallbackButton = new NativeContextMenus.MenuButton
        {
            Frame = new CGRect(isRightToLeft ? 0 : bar.Bounds.Width - size, 0, size, bar.Bounds.Height),
            IgnoresTouches = true,
            IsAccessibilityElement = false,
            ShowsMenuAsPrimaryAction = true,
            Menu = CreateMenu(),
        };

        bar.AddSubview(fallbackButton);
        fallbackButton.PerformPrimaryAction();
    }
}
