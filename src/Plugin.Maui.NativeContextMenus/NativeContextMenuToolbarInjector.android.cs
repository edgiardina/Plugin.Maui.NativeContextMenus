#if ANDROID
using Android.Views;
using AndroidX.AppCompat.Widget;
using System.Collections.Concurrent;
using Microsoft.Maui.Graphics;
using AndroidView = Android.Views.View;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// Android platform injector for NativeContextMenuToolbarItem.
/// Converts MenuNode hierarchies into Toolbar SubMenu structures.
/// </summary>
public static class NativeContextMenuToolbarInjector
{
    private static readonly ConcurrentDictionary<string, int> _groupIdCache = new();
    private static int _nextGroupId = 1000; // Start high to avoid conflicts

    /// <summary>
    /// Apply menu toolbar items to an Android Toolbar Menu by adding SubMenus
    /// for each NativeContextMenuToolbarItem.
    /// </summary>
    public static void ApplyTo(IMenu menu, IList<ToolbarItem> toolbarItems)
    {
        if (menu == null || toolbarItems == null) return;

        int order = 0;

        foreach (var item in toolbarItems)
        {
            if (item is NativeContextMenuToolbarItem menuItem)
            {
                AddMenuToolbarItem(menu, menuItem, order++);
            }
            else
            {
                AddRegularToolbarItem(menu, item, order++);
            }
        }
    }

    private static void AddMenuToolbarItem(IMenu menu, NativeContextMenuToolbarItem menuItem, int order)
    {
        var roots = menuItem.GetActiveRoots();
        if (!roots.Any())
        {
            // Add disabled placeholder
            var placeholder = menu.Add(Menu.None, AndroidView.GenerateViewId(), order, menuItem.Text ?? "No items");
            placeholder.SetEnabled(false);
            return;
        }

        // Create a top-level menu item that will show submenu
        var topItem = menu.Add(Menu.None, AndroidView.GenerateViewId(), order, menuItem.Text ?? "Menu");
        
        // Set icon if available
        if (menuItem.IconImageSource != null)
        {
            var drawable = ConvertImageSource(menuItem.IconImageSource);
            if (drawable != null)
            {
                topItem.SetIcon(drawable);
            }
        }

        // Create submenu
        var subMenu = topItem.SubMenu;
        if (subMenu != null)
        {
            BuildSubMenu(subMenu, roots, menuItem);
        }
        else
        {
            // If we can't create submenu, set up click listener to use inherited Command
            topItem.SetOnMenuItemClickListener(new ToolbarItemClickListener(menuItem));
        }

        // Set up rebuild hook
        menuItem.RequestRebuildHook = () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (subMenu != null)
                {
                    subMenu.Clear();
                    BuildSubMenu(subMenu, menuItem.GetActiveRoots(), menuItem);
                }
            });
        };
    }

    private static void AddRegularToolbarItem(IMenu menu, ToolbarItem item, int order)
    {
        var menuItem = menu.Add(Menu.None, AndroidView.GenerateViewId(), order, item.Text ?? "");
        
        if (item.IconImageSource != null)
        {
            var drawable = ConvertImageSource(item.IconImageSource);
            if (drawable != null)
            {
                menuItem.SetIcon(drawable);
            }
        }

        menuItem.SetOnMenuItemClickListener(new MenuItemClickListener(item));
    }

    private static void BuildSubMenu(ISubMenu subMenu, IEnumerable<MenuNode> nodes, NativeContextMenuToolbarItem owner)
    {
        int order = 0;
        var processedGroupKeys = new HashSet<string>();

        foreach (var node in nodes)
        {
            BuildMenuItem(subMenu, node, order++, owner, processedGroupKeys);
        }
    }

    private static void BuildMenuItem(ISubMenu subMenu, MenuNode node, int order, NativeContextMenuToolbarItem owner, HashSet<string> processedGroupKeys)
    {
        if (node.Children.Any())
        {
            // Create submenu
            var menuItem = subMenu.Add(Menu.None, AndroidView.GenerateViewId(), order, node.Title ?? "");
            
            if (node.Icon != null)
            {
                var drawable = ConvertImageSource(node.Icon);
                if (drawable != null)
                {
                    menuItem.SetIcon(drawable);
                }
            }

            var childSubMenu = menuItem.SubMenu;
            if (childSubMenu != null)
            {
                var childProcessedGroups = new HashSet<string>();
                int childOrder = 0;
                foreach (var child in node.Children)
                {
                    BuildMenuItem(childSubMenu, child, childOrder++, owner, childProcessedGroups);
                }
            }
        }
        else
        {
            // Create leaf menu item
            int groupId = Menu.None;
            
            // Handle radio groups
            if (node.IsCheckable && !string.IsNullOrEmpty(node.GroupKey))
            {
                groupId = GetGroupId(node.GroupKey);
                
                // Set up radio group if this is the first item in the group
                if (!processedGroupKeys.Contains(node.GroupKey))
                {
                    subMenu.SetGroupCheckable(groupId, true, true);
                    processedGroupKeys.Add(node.GroupKey);
                }
            }

            var menuItem = subMenu.Add(groupId, AndroidView.GenerateViewId(), order, node.Title ?? "");
            
            menuItem.SetEnabled(node.IsEnabled);
            
            if (node.Icon != null)
            {
                var drawable = ConvertImageSource(node.Icon);
                if (drawable != null)
                {
                    menuItem.SetIcon(drawable);
                }
            }

            // Set checked state
            if (node.IsCheckable)
            {
                menuItem.SetCheckable(true);
                menuItem.SetChecked(node.IsChecked);
            }

            // Set click listener
            menuItem.SetOnMenuItemClickListener(new MenuNodeClickListener(node, owner));
        }
    }

    private static int GetGroupId(string groupKey)
    {
        return _groupIdCache.GetOrAdd(groupKey, _ => _nextGroupId++);
    }

    private static Android.Graphics.Drawables.Drawable? ConvertImageSource(ImageSource? imageSource)
    {
        if (imageSource == null) return null;

        try
        {
            if (imageSource is FileImageSource fileSource)
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                var resourceId = context.Resources?.GetIdentifier(
                    System.IO.Path.GetFileNameWithoutExtension(fileSource.File),
                    "drawable",
                    context.PackageName) ?? 0;
                
                if (resourceId != 0)
                {
                    return AndroidX.Core.Content.ContextCompat.GetDrawable(context, resourceId);
                }
            }
            else if (imageSource is FontImageSource fontSource)
            {
                // For simplicity, we'll skip font icons in Android menus for now
                // Android menu icons are typically small and font icons may not render well
                return null;
            }
        }
        catch
        {
            // If image conversion fails, continue without image
        }

        return null;
    }

    private class MenuItemClickListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener
    {
        private readonly ToolbarItem _item;

        public MenuItemClickListener(ToolbarItem item)
        {
            _item = item;
        }

        public bool OnMenuItemClick(IMenuItem? item)
        {
            if (_item.Command?.CanExecute(_item.CommandParameter) == true)
            {
                _item.Command.Execute(_item.CommandParameter);
            }
            return true;
        }
    }

    private class MenuNodeClickListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener
    {
        private readonly MenuNode _node;
        private readonly NativeContextMenuToolbarItem _owner;

        public MenuNodeClickListener(MenuNode node, NativeContextMenuToolbarItem owner)
        {
            _node = node;
            _owner = owner;
        }

        public bool OnMenuItemClick(IMenuItem? item)
        {
            // Handle radio group logic
            if (_node.IsCheckable && !string.IsNullOrEmpty(_node.GroupKey))
            {
                ApplyRadioGroupSelection(_node, _owner.GetActiveRoots());
            }

            // Execute node command
            if (_node.Command?.CanExecute(_node.CommandParameter) == true)
            {
                _node.Command.Execute(_node.CommandParameter);
            }

            // Raise events
            _node.RaiseTapped();
            _owner.RaiseItemTapped(_node);

            // Request rebuild to refresh menu
            _owner.RequestRebuild();

            return true;
        }
    }

    private class ToolbarItemClickListener : Java.Lang.Object, IMenuItemOnMenuItemClickListener
    {
        private readonly NativeContextMenuToolbarItem _item;

        public ToolbarItemClickListener(NativeContextMenuToolbarItem item)
        {
            _item = item;
        }

        public bool OnMenuItemClick(IMenuItem? item)
        {
            // Use the inherited Command for fallback behavior
            if (_item.Command?.CanExecute(_item.CommandParameter) == true)
            {
                _item.Command.Execute(_item.CommandParameter);
            }
            return true;
        }
    }

    private static void ApplyRadioGroupSelection(MenuNode selectedNode, IEnumerable<MenuNode> roots)
    {
        if (string.IsNullOrEmpty(selectedNode.GroupKey)) return;

        // Find all nodes with the same GroupKey and uncheck them
        var allNodes = GetAllNodes(roots);
        var groupNodes = allNodes.Where(n => n.GroupKey == selectedNode.GroupKey && n.IsCheckable).ToList();

        foreach (var node in groupNodes)
        {
            node.IsChecked = node == selectedNode;
        }
    }

    private static IEnumerable<MenuNode> GetAllNodes(IEnumerable<MenuNode> roots)
    {
        foreach (var root in roots)
        {
            yield return root;
            foreach (var child in GetAllNodes(root.Children))
            {
                yield return child;
            }
        }
    }
}
#endif