using System.Runtime.CompilerServices;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;
using APaint = Android.Graphics.Paint;
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
        var builder = new PopupBuilder(context, element.Handler?.MauiContext);
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

    sealed class PopupBuilder(Context context, IMauiContext? mauiContext)
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
                if (ToDrawable(node) is { } icon)
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

        // Icon has priority. If it gives no image, the bundled drawable for SystemIcon is used.
        Drawable? ToDrawable(MenuNode node)
        {
            var drawable = node.Icon switch
            {
                FileImageSource { File: { Length: > 0 } file } => GetDrawable(
                    context.Resources?.GetIdentifier(System.IO.Path.GetFileNameWithoutExtension(file).ToLowerInvariant(), "drawable", context.PackageName) ?? 0),
                FontImageSource { Glyph: { Length: > 0 } } font when mauiContext is not null => RenderGlyph(font, mauiContext),
                _ => null,
            };

            return drawable ?? GetDrawable(SystemIconDrawables.Id(node.SystemIcon));
        }

        Drawable? GetDrawable(int id) => id == 0 ? null : context.GetDrawable(id);

        // Menu icons are 24dp. With no color set, the glyph gets the icon color of the theme.
        Drawable? RenderGlyph(FontImageSource source, IMauiContext mauiContext)
        {
            using var paint = new APaint(PaintFlags.AntiAlias)
            {
                TextSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 24, context.Resources?.DisplayMetrics),
                Color = source.Color?.ToPlatform() ?? ThemeIconColor(),
            };
            paint.SetTypeface(mauiContext.Services.GetRequiredService<IFontManager>()
                .GetTypeface(Microsoft.Maui.Font.OfSize(source.FontFamily, source.Size)));

            var width = (int)Math.Ceiling(paint.MeasureText(source.Glyph));
            var height = (int)Math.Ceiling(paint.Descent() - paint.Ascent());
            if (width <= 0 || height <= 0)
                return null;

            var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);
            using var canvas = new Canvas(bitmap);
            canvas.DrawText(source.Glyph, 0, -paint.Ascent(), paint);
            return new BitmapDrawable(context.Resources, bitmap);
        }

        AColor ThemeIconColor()
        {
            using var value = new TypedValue();
            if (context.Theme?.ResolveAttribute(Android.Resource.Attribute.ColorControlNormal, value, true) != true)
                return AColor.Gray;

            return value.ResourceId != 0 ? new AColor(context.GetColor(value.ResourceId)) : new AColor(value.Data);
        }
    }
}
