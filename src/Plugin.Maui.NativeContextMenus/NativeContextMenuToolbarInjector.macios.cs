#if IOS || MACCATALYST
using UIKit;
using Foundation;
using System.Collections.Concurrent;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// iOS/macOS platform injector for NativeContextMenuToolbarItem.
/// Converts MenuNode hierarchies into UIMenu/UIAction trees.
/// </summary>
public static class NativeContextMenuToolbarInjector
{
    // Cache for converted images to avoid repeated conversions
    private static readonly ConcurrentDictionary<string, UIImage?> _imageCache = new();

    /// <summary>
    /// Apply menu toolbar items to a UINavigationItem by replacing regular ToolbarItems
    /// with UIBarButtonItems that have UIMenu support.
    /// </summary>
    public static void ApplyTo(UINavigationItem navigationItem, IList<ToolbarItem> toolbarItems)
    {
        if (navigationItem == null || toolbarItems == null) return;

        var rightItems = new List<UIBarButtonItem>();

        foreach (var item in toolbarItems)
        {
            if (item is NativeContextMenuToolbarItem menuItem)
            {
                var barButtonItem = CreateMenuBarButtonItem(menuItem);
                rightItems.Add(barButtonItem);
            }
            else
            {
                // Convert regular ToolbarItem to UIBarButtonItem
                var barButtonItem = CreateRegularBarButtonItem(item);
                rightItems.Add(barButtonItem);
            }
        }

        navigationItem.SetRightBarButtonItems(rightItems.ToArray(), false);
    }

    private static UIBarButtonItem CreateMenuBarButtonItem(NativeContextMenuToolbarItem menuItem)
    {
        var barButtonItem = new UIBarButtonItem();
        
        // Set basic properties
        barButtonItem.Title = menuItem.Text;
        if (menuItem.IconImageSource != null)
        {
            barButtonItem.Image = ConvertImageSource(menuItem.IconImageSource);
        }

        // Check if iOS 14+ for menu support
        if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
        {
            // Build the menu
            var menu = BuildMenu(menuItem);
            
            // Use reflection to set ShowsMenuAsPrimaryAction if available
            try
            {
                var property = typeof(UIBarButtonItem).GetProperty("ShowsMenuAsPrimaryAction");
                property?.SetValue(barButtonItem, true);
                
                var menuProperty = typeof(UIBarButtonItem).GetProperty("Menu");
                menuProperty?.SetValue(barButtonItem, menu);
            }
            catch
            {
                // Fallback to regular button behavior
                SetupFallbackAction(barButtonItem, menuItem);
            }
        }
        else
        {
            // iOS 13 and below - fallback to action
            SetupFallbackAction(barButtonItem, menuItem);
        }

        // Set up rebuild hook so the menu can be refreshed when data changes
        menuItem.RequestRebuildHook = () =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
                {
                    try
                    {
                        var menu = BuildMenu(menuItem);
                        var menuProperty = typeof(UIBarButtonItem).GetProperty("Menu");
                        menuProperty?.SetValue(barButtonItem, menu);
                    }
                    catch { }
                }
            });
        };

        return barButtonItem;
    }

    private static void SetupFallbackAction(UIBarButtonItem barButtonItem, NativeContextMenuToolbarItem menuItem)
    {
        barButtonItem.Clicked += (s, e) =>
        {
            // Use the inherited Command if available
            if (menuItem.Command?.CanExecute(menuItem.CommandParameter) == true)
            {
                menuItem.Command.Execute(menuItem.CommandParameter);
                return;
            }

            // Otherwise fall back to first available menu command (iOS 13 behavior)
            var roots = menuItem.GetActiveRoots();
            var firstLeaf = GetFirstLeafNode(roots);
            if (firstLeaf != null)
            {
                if (firstLeaf.Command?.CanExecute(firstLeaf.CommandParameter) == true)
                {
                    firstLeaf.Command.Execute(firstLeaf.CommandParameter);
                }
                firstLeaf.RaiseTapped();
                menuItem.RaiseItemTapped(firstLeaf);
            }
        };
    }

    private static MenuNode? GetFirstLeafNode(IEnumerable<MenuNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!node.Children.Any())
                return node;
            
            var leaf = GetFirstLeafNode(node.Children);
            if (leaf != null)
                return leaf;
        }
        return null;
    }

    private static UIBarButtonItem CreateRegularBarButtonItem(ToolbarItem item)
    {
        var barButtonItem = new UIBarButtonItem();
        barButtonItem.Title = item.Text;
        
        if (item.IconImageSource != null)
        {
            barButtonItem.Image = ConvertImageSource(item.IconImageSource);
        }

        if (item.Command != null)
        {
            barButtonItem.Clicked += (s, e) =>
            {
                if (item.Command.CanExecute(item.CommandParameter))
                {
                    item.Command.Execute(item.CommandParameter);
                }
            };
        }

        return barButtonItem;
    }

    private static object? BuildMenu(NativeContextMenuToolbarItem menuItem)
    {
        if (!UIDevice.CurrentDevice.CheckSystemVersion(14, 0))
            return null;

        var roots = menuItem.GetActiveRoots();
        if (!roots.Any())
        {
            return CreateEmptyMenu();
        }

        var menuElements = new List<object>();
        
        // Check if first group should be displayed inline (for iOS thumbnail row effect)
        bool isFirstGroup = true;

        foreach (var rootNode in roots)
        {
            var element = BuildMenuElement(rootNode, menuItem, isFirstGroup);
            if (element != null)
            {
                menuElements.Add(element);
                isFirstGroup = false;
            }
        }

        return CreateUIMenu("", null, menuElements.ToArray());
    }

    private static object? BuildMenuElement(MenuNode node, NativeContextMenuToolbarItem owner, bool isFirstGroup = false)
    {
        if (!node.IsEnabled && !node.Children.Any())
        {
            return null; // Skip disabled leaf nodes
        }

        if (node.Children.Any())
        {
            // This is a submenu
            var childElements = new List<object>();
            
            foreach (var child in node.Children)
            {
                var childElement = BuildMenuElement(child, owner, false);
                if (childElement != null)
                {
                    childElements.Add(childElement);
                }
            }

            if (!childElements.Any())
            {
                return null; // Skip empty submenus
            }

            // Check if all children are checkable with same GroupKey (radio group)
            var checkableChildren = node.Children.Where(c => c.IsCheckable && !string.IsNullOrEmpty(c.GroupKey)).ToList();
            bool isSingleSelection = checkableChildren.Any() && checkableChildren.All(c => c.GroupKey == checkableChildren.First().GroupKey);

            return CreateUIMenu(node.Title ?? "", isSingleSelection, childElements.ToArray());
        }
        else
        {
            // This is a leaf action
            return CreateUIAction(node, owner);
        }
    }

    private static object CreateUIAction(MenuNode node, NativeContextMenuToolbarItem owner)
    {
        // Use reflection to create UIAction since we need iOS 13+ APIs
        try
        {
            var uiActionType = Type.GetType("UIKit.UIAction, Xamarin.iOS") ?? 
                              Type.GetType("UIKit.UIAction, Microsoft.iOS");
            
            if (uiActionType == null)
                return CreateFallbackAction(node, owner);

            // Create UIAction
            var createMethod = uiActionType.GetMethod("Create", new[] { typeof(string), typeof(UIImage), typeof(object), typeof(Action<object>) });
            if (createMethod == null)
                return CreateFallbackAction(node, owner);

            var action = createMethod.Invoke(null, new object[] {
                node.Title ?? "",
                ConvertImageSource(node.Icon),
                GetActionAttributes(node),
                new Action<object>(_ => HandleNodeTapped(node, owner))
            });

            // Set state for checkable items
            if (node.IsCheckable && action != null)
            {
                var stateProperty = uiActionType.GetProperty("State");
                if (stateProperty != null)
                {
                    // UIMenuElementState.On = 1, Off = 0
                    var state = node.IsChecked ? 1 : 0;
                    stateProperty.SetValue(action, state);
                }
            }

            return action;
        }
        catch
        {
            return CreateFallbackAction(node, owner);
        }
    }

    private static object CreateFallbackAction(MenuNode node, NativeContextMenuToolbarItem owner)
    {
        // Create a simple dictionary representation for fallback
        return new Dictionary<string, object>
        {
            ["Title"] = node.Title ?? "",
            ["Enabled"] = node.IsEnabled,
            ["Action"] = new Action(() => HandleNodeTapped(node, owner))
        };
    }

    private static object GetActionAttributes(MenuNode node)
    {
        // UIMenuElementAttributes flags
        int attributes = 0; // None = 0
        
        if (!node.IsEnabled)
            attributes |= 1; // Disabled = 1
        
        if (node.Destructive)
            attributes |= 2; // Destructive = 2
            
        if (node.KeepMenuOpen)
            attributes |= 4; // KeepsMenuPresented = 4

        return attributes;
    }

    private static object CreateUIMenu(string title, bool? singleSelection, object[] children)
    {
        try
        {
            var uiMenuType = Type.GetType("UIKit.UIMenu, Xamarin.iOS") ?? 
                            Type.GetType("UIKit.UIMenu, Microsoft.iOS");
            
            if (uiMenuType == null)
                return CreateFallbackMenu(title, children);

            // Get menu options
            int options = 0; // None = 0
            if (singleSelection == true)
                options |= 1; // SingleSelection = 1

            var createMethod = uiMenuType.GetMethod("Create", new[] { typeof(string), typeof(string), typeof(object), typeof(int), typeof(object[]) });
            if (createMethod != null)
            {
                return createMethod.Invoke(null, new object[] { title, null, null, options, children });
            }

            return CreateFallbackMenu(title, children);
        }
        catch
        {
            return CreateFallbackMenu(title, children);
        }
    }

    private static object CreateEmptyMenu()
    {
        var emptyAction = CreateUIAction(new MenuNode { Title = "No items", IsEnabled = false }, null);
        return CreateUIMenu("", false, new[] { emptyAction });
    }

    private static object CreateFallbackMenu(string title, object[] children)
    {
        return new Dictionary<string, object>
        {
            ["Title"] = title,
            ["Children"] = children
        };
    }

    private static void HandleNodeTapped(MenuNode node, NativeContextMenuToolbarItem owner)
    {
        // Handle radio group logic
        if (node.IsCheckable && !string.IsNullOrEmpty(node.GroupKey))
        {
            ApplyRadioGroupSelection(node, owner.GetActiveRoots());
        }

        // Execute node command
        if (node.Command?.CanExecute(node.CommandParameter) == true)
        {
            node.Command.Execute(node.CommandParameter);
        }

        // Raise node tapped event
        node.RaiseTapped();

        // Raise owner item tapped
        owner.RaiseItemTapped(node);

        // Request rebuild to refresh checkmarks and titles
        owner.RequestRebuild();
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

    private static UIImage? ConvertImageSource(ImageSource? imageSource)
    {
        if (imageSource == null) return null;

        // Use cache key for performance
        var cacheKey = imageSource.ToString() ?? "";
        
        if (_imageCache.TryGetValue(cacheKey, out var cachedImage))
        {
            return cachedImage;
        }

        UIImage? image = null;

        try
        {
            if (imageSource is FileImageSource fileSource)
            {
                image = UIImage.FromBundle(fileSource.File);
            }
            else if (imageSource is FontImageSource fontSource)
            {
                // Convert FontImageSource to UIImage
                image = CreateImageFromFont(fontSource);
            }
            // Skip stream images for now as they're complex in menu contexts
        }
        catch
        {
            // If image conversion fails, continue without image
            image = null;
        }

        _imageCache.TryAdd(cacheKey, image);
        return image;
    }

    private static UIImage? CreateImageFromFont(FontImageSource fontSource)
    {
        try
        {
            var fontSize = (nfloat)(fontSource.Size > 0 ? fontSource.Size : 22);
            var font = UIFont.SystemFontOfSize(fontSize);

            if (!string.IsNullOrEmpty(fontSource.FontFamily))
            {
                font = UIFont.FromName(fontSource.FontFamily, fontSize) ?? font;
            }

            // Convert MAUI color to UIColor
            var color = UIColor.Label; // Default color
            if (fontSource.Color != null)
            {
                var c = fontSource.Color;
                color = UIColor.FromRGBA((nfloat)c.Red, (nfloat)c.Green, (nfloat)c.Blue, (nfloat)c.Alpha);
            }

            var glyph = fontSource.Glyph ?? "";

            var attributes = new UIStringAttributes
            {
                Font = font,
                ForegroundColor = color
            };

            var size = new Foundation.NSString(glyph).GetSizeUsingAttributes(attributes);
            var rect = new CoreGraphics.CGRect(0, 0, size.Width, size.Height);

            UIGraphics.BeginImageContextWithOptions(size, false, 0);
            new Foundation.NSString(glyph).DrawString(rect, attributes);
            var image = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            return image;
        }
        catch
        {
            return null;
        }
    }
}
#endif