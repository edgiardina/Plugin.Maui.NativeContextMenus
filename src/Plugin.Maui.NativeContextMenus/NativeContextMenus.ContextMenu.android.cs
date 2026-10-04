using System.Runtime.CompilerServices;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AView = Android.Views.View;

namespace Plugin.Maui.NativeContextMenus;

public static partial class NativeContextMenus
{
    static readonly ConditionalWeakTable<AView, TriggerListener> listeners = new();

    static partial void PlatformAttach(VisualElement element, object platformView)
    {
        if (platformView is not AView view ||
            listeners.TryGetValue(view, out _) ||
            GetContextMenu(element) is not { } menu)
            return;

        var listener = new TriggerListener(element, menu.Trigger);
        if (listener.Trigger == ContextMenuTrigger.Tap)
            view.SetOnClickListener(listener);
        else
            view.SetOnLongClickListener(listener);
        listeners.Add(view, listener);
    }

    static partial void PlatformDetach(object? platformView)
    {
        if (platformView is not AView view || !listeners.TryGetValue(view, out var listener))
            return;

        if (listener.Trigger == ContextMenuTrigger.Tap)
        {
            view.SetOnClickListener(null);
            view.Clickable = false;
        }
        else
        {
            view.SetOnLongClickListener(null);
            view.LongClickable = false;
        }

        listeners.Remove(view);
    }

    static partial void PlatformShow(VisualElement element, object platformView)
    {
        if (platformView is AView view)
            ShowPopup(view, element);
    }

    static bool ShowPopup(AView anchor, VisualElement element)
    {
        if (anchor.Context is not { } context ||
            GetContextMenu(element) is not { HasVisibleItems: true } menu)
            return false;

        var popup = new PopupMenu(context, anchor);
        if (popup.Menu is not { } root)
            return false;

        // The menu is built each time it opens, so it always shows the current node state
        var builder = new PopupBuilder(context);
        builder.AddNodes(root, menu.Items);

        if (OperatingSystem.IsAndroidVersionAtLeast(28))
            root.SetGroupDividerEnabled(true);
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            popup.SetForceShowIcon(true);

        popup.MenuItemClick += (_, e) =>
        {
            if (e.Item is { } item && builder.Leaves.TryGetValue(item.ItemId, out var node))
                menu.Activate(node);
        };

        popup.Show();
        return true;
    }

    sealed class TriggerListener(VisualElement element, ContextMenuTrigger trigger)
        : Java.Lang.Object, AView.IOnClickListener, AView.IOnLongClickListener
    {
        readonly WeakReference<VisualElement> element = new(element);

        public ContextMenuTrigger Trigger { get; } = trigger;

        public void OnClick(AView? v) => OnLongClick(v);

        public bool OnLongClick(AView? v) =>
            v is not null && element.TryGetTarget(out var target) && ShowPopup(v, target);
    }

    sealed class PopupBuilder(Context context)
    {
        int lastItemId;
        int lastGroupId;

        public Dictionary<int, MenuNode> Leaves { get; } = new();

        public void AddNodes(IMenu target, IEnumerable<MenuNode> nodes)
        {
            // Android draws a divider where the group id changes, so each section gets its own group
            var groupId = ++lastGroupId;
            var group = new List<(IMenuItem Item, MenuNode Node)>();

            foreach (var node in nodes.Where(n => n.IsVisible))
            {
                if (node.IsSection)
                {
                    ApplyRadioGroup(target, groupId, group);
                    AddNodes(target, node.Children);
                    groupId = ++lastGroupId;
                    group = new();
                    continue;
                }

                var itemId = ++lastItemId;
                IMenuItem? item;

                if (node.Children.Count > 0)
                {
                    var subMenu = target.AddSubMenu(groupId, itemId, IMenu.None, node.Title ?? string.Empty);
                    if (subMenu is null)
                        continue;

                    AddNodes(subMenu, node.Children);
                    item = subMenu.Item;
                }
                else
                {
                    item = target.Add(groupId, itemId, IMenu.None, node.Title ?? string.Empty);
                    item?.SetCheckable(node.IsCheckable);
                    item?.SetChecked(node.IsCheckable && node.IsChecked);
                    Leaves[itemId] = node;
                }

                if (item is null)
                    continue;

                item.SetEnabled(node.IsEnabled);
                if (ToDrawable(node.Icon) is { } icon)
                    item.SetIcon(icon);

                group.Add((item, node));
            }

            ApplyRadioGroup(target, groupId, group);
        }

        // Shows radio buttons, not check boxes, when the full group is one radio group
        static void ApplyRadioGroup(IMenu target, int groupId, List<(IMenuItem Item, MenuNode Node)> group)
        {
            if (!MenuTree.IsRadioGroup(group.Select(g => g.Node).ToList()))
                return;

            target.SetGroupCheckable(groupId, true, true);
            foreach (var (item, node) in group)
                if (node.IsChecked)
                    item.SetChecked(true);
        }

        // Only drawable resources (which include MauiImage files) are supported
        Drawable? ToDrawable(ImageSource? source)
        {
            if (source is not FileImageSource { File: { Length: > 0 } file })
                return null;

            var name = System.IO.Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            var id = context.Resources?.GetIdentifier(name, "drawable", context.PackageName) ?? 0;
            return id == 0 ? null : context.GetDrawable(id);
        }
    }
}
