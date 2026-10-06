using Android.Views;
using AToolbar = AndroidX.AppCompat.Widget.Toolbar;
using AView = Android.Views.View;

namespace Plugin.Maui.NativeContextMenus;

public partial class NativeContextMenuToolbarItem
{
    // .NET MAUI calls OnClicked when the user taps the toolbar button
    partial void PlatformClicked()
    {
        if (FindPage()?.Handler is not { PlatformView: AView pageView } handler ||
            FindToolbar(pageView) is not { } toolbar)
            return;

        // An item in the overflow menu has no button, so the menu opens at the end of the toolbar
        if (FindButton(toolbar) is { } button)
            NativeContextMenus.ShowPopup(button, this, handler.MauiContext);
        else
            NativeContextMenus.ShowPopup(toolbar, this, handler.MauiContext, GravityFlags.End);
    }

    // The toolbar of a page is not an ancestor of the page view, so each ancestor is searched
    static AToolbar? FindToolbar(AView pageView)
    {
        for (AView? view = pageView; view is not null; view = view.Parent as AView)
            if (FindShownToolbar(view) is { } toolbar)
                return toolbar;

        return null;
    }

    static AToolbar? FindShownToolbar(AView view)
    {
        if (view is AToolbar toolbar)
            return toolbar.IsShown ? toolbar : null;

        if (view is ViewGroup group)
            for (var i = 0; i < group.ChildCount; i++)
                if (group.GetChildAt(i) is { } child && FindShownToolbar(child) is { } found)
                    return found;

        return null;
    }

    // .NET MAUI gives each toolbar button the id of its menu item, and the menu item has the Text of the ToolbarItem
    AView? FindButton(AToolbar toolbar)
    {
        if (toolbar.Menu is not { } menu)
            return null;

        var text = Text ?? string.Empty;
        for (var i = 0; i < menu.Size(); i++)
            if (menu.GetItem(i) is { } item &&
                (item.TitleFormatted?.ToString() ?? string.Empty) == text &&
                toolbar.FindViewById(item.ItemId) is { } button)
                return button;

        return null;
    }
}
