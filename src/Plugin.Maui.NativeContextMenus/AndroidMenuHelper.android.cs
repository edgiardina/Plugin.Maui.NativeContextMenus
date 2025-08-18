#if ANDROID
using Android.Views;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// Android utility class for manually injecting NativeContextMenuToolbarItem into Activity menus.
/// Call this from your Activity's OnCreateOptionsMenu or OnPrepareOptionsMenu methods.
/// </summary>
public static class AndroidMenuHelper
{
    /// <summary>
    /// Inject toolbar menu items into an Android Menu.
    /// Call this from your Activity's OnCreateOptionsMenu method.
    /// </summary>
    /// <param name="menu">The Android menu</param>
    /// <param name="page">The ContentPage with toolbar items</param>
    /// <returns>True if any menu items were injected</returns>
    public static bool InjectToolbarMenuItems(IMenu menu, ContentPage page)
    {
        if (menu == null || page?.ToolbarItems == null) return false;

        var hasMenuItems = page.ToolbarItems.Any(item => item is NativeContextMenuToolbarItem);
        if (hasMenuItems)
        {
            NativeContextMenuToolbarInjector.ApplyTo(menu, page.ToolbarItems);
        }

        return hasMenuItems;
    }

    /// <summary>
    /// Check if a page has any NativeContextMenuToolbarItems that need injection.
    /// </summary>
    /// <param name="page">The ContentPage to check</param>
    /// <returns>True if the page has menu toolbar items</returns>
    public static bool HasMenuToolbarItems(ContentPage page)
    {
        return page?.ToolbarItems?.Any(item => item is NativeContextMenuToolbarItem) == true;
    }
}
#endif