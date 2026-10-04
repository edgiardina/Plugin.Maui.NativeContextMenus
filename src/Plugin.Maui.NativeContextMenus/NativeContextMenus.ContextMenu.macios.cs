using System.Runtime.CompilerServices;
using CoreGraphics;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Platform;
using UIKit;

namespace Plugin.Maui.NativeContextMenus;

public static partial class NativeContextMenus
{
    // The interaction holds its delegate weakly, so the table keeps the delegate alive with the view
    static readonly ConditionalWeakTable<UIView, Attachment> attachments = new();

    static partial void PlatformAttach(VisualElement element, object platformView)
    {
        if (platformView is not UIView view ||
            attachments.TryGetValue(view, out _) ||
            GetContextMenu(element) is not { } menu)
            return;

        var attachment = new Attachment(element, view, menu.Trigger);
        view.UserInteractionEnabled = true;
        view.AddSubview(attachment.Button);
        if (attachment.Interaction is not null)
            view.AddInteraction(attachment.Interaction);
        attachments.Add(view, attachment);
    }

    static partial void PlatformDetach(object? platformView)
    {
        if (platformView is not UIView view || !attachments.TryGetValue(view, out var attachment))
            return;

        attachment.Button.RemoveFromSuperview();
        if (attachment.Interaction is not null)
            view.RemoveInteraction(attachment.Interaction);
        attachments.Remove(view);
    }

    static partial void PlatformShow(VisualElement element, object platformView)
    {
        if (platformView is UIView view && attachments.TryGetValue(view, out var attachment))
            attachment.Button.PerformPrimaryAction();
    }

    // The elements are built each time the menu opens, so the menu always shows the current node state
    static UIMenuElement[] BuildElements(WeakReference<VisualElement> element) =>
        element.TryGetTarget(out var target) && GetContextMenu(target) is { HasVisibleItems: true } menu
            ? BuildElements(menu.Items, menu, target.Handler?.MauiContext)
            : [];

    static string GetTitle(WeakReference<VisualElement> element) =>
        element.TryGetTarget(out var target) ? GetContextMenu(target)?.Title ?? string.Empty : string.Empty;

    sealed class Attachment
    {
        readonly InteractionDelegate? interactionDelegate;

        // UIKit can open a menu on tap or from code only for a button, so each view gets a clear button.
        // The button ignores touches unless the trigger is Tap.
        public MenuButton Button { get; }

        public UIContextMenuInteraction? Interaction { get; }

        public Attachment(VisualElement element, UIView view, ContextMenuTrigger trigger)
        {
            var weakElement = new WeakReference<VisualElement>(element);

            var deferred = UIDeferredMenuElement.CreateUncached(completion => completion(BuildElements(weakElement)));

            Button = new MenuButton
            {
                Frame = view.Bounds,
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
                IgnoresTouches = trigger != ContextMenuTrigger.Tap,
                IsAccessibilityElement = false,
                ShowsMenuAsPrimaryAction = true,
                Menu = UIMenu.Create(GetTitle(weakElement), [deferred]),
            };

            if (trigger == ContextMenuTrigger.LongPress)
            {
                interactionDelegate = new InteractionDelegate(weakElement);
                Interaction = new UIContextMenuInteraction(interactionDelegate);
            }
        }
    }

    sealed class MenuButton : UIButton
    {
        public bool IgnoresTouches { get; set; }

        public override bool PointInside(CGPoint point, UIEvent? uievent) =>
            !IgnoresTouches && base.PointInside(point, uievent);
    }

    sealed class InteractionDelegate(WeakReference<VisualElement> element) : UIContextMenuInteractionDelegate
    {
        public override UIContextMenuConfiguration? GetConfigurationForMenu(UIContextMenuInteraction interaction, CGPoint location)
        {
            if (BuildElements(element).Length == 0)
                return null;

            return UIContextMenuConfiguration.Create(
                identifier: null,
                previewProvider: null,
                actionProvider: _ => UIMenu.Create(GetTitle(element), BuildElements(element)));
        }
    }

    static UIMenuElement[] BuildElements(IEnumerable<MenuNode> nodes, NativeContextMenu owner, IMauiContext? mauiContext) =>
        nodes.Where(n => n.IsVisible).Select(n => BuildElement(n, owner, mauiContext)).ToArray();

    static UIMenuElement BuildElement(MenuNode node, NativeContextMenu owner, IMauiContext? mauiContext)
    {
        var title = node.Title ?? string.Empty;
        var image = ToUIImage(node, mauiContext);

        if (node.Children.Count > 0)
        {
            var options = (UIMenuOptions)0;
            if (node.IsSection)
                options |= UIMenuOptions.DisplayInline;
            if (node.Destructive)
                options |= UIMenuOptions.Destructive;

            return UIMenu.Create(title, image, UIMenuIdentifier.None, options, BuildElements(node.Children, owner, mauiContext));
        }

        var action = UIAction.Create(title, image, null, _ => owner.Activate(node));

        var attributes = (UIMenuElementAttributes)0;
        if (!node.IsEnabled)
            attributes |= UIMenuElementAttributes.Disabled;
        if (node.Destructive)
            attributes |= UIMenuElementAttributes.Destructive;
        if (node.KeepMenuOpen)
            attributes |= UIMenuElementAttributes.KeepsMenuPresented;
        action.Attributes = attributes;

        action.State = node.IsCheckable && node.IsChecked ? UIMenuElementState.On : UIMenuElementState.Off;
        return action;
    }

    static UIImage? ToUIImage(MenuNode node, IMauiContext? mauiContext)
    {
        var image = node.Icon switch
        {
            FileImageSource { File: { Length: > 0 } file } => UIImage.FromBundle(file),
            FontImageSource { Glyph: { Length: > 0 } } font when mauiContext is not null => RenderGlyph(font, mauiContext),
            _ => null,
        };

        if (image is not null)
            return image;

        var symbol = string.IsNullOrEmpty(node.SymbolName) ? SystemIconSymbols.Name(node.SystemIcon) : node.SymbolName;
        return symbol is null ? null : UIImage.GetSystemImage(symbol);
    }

    static UIImage RenderGlyph(FontImageSource source, IMauiContext mauiContext)
    {
        var fontManager = mauiContext.Services.GetRequiredService<IFontManager>();
        var attributes = new UIStringAttributes
        {
            Font = fontManager.GetFont(Microsoft.Maui.Font.OfSize(source.FontFamily, source.Size)),
            ForegroundColor = source.Color?.ToPlatform() ?? UIColor.Label,
        };

        var glyph = new NSString(source.Glyph);
        var renderer = new UIGraphicsImageRenderer(glyph.GetSizeUsingAttributes(attributes));
        var image = renderer.CreateImage(_ => glyph.DrawString(CGPoint.Empty, attributes));

        // With no color set, the system tints the glyph the same as an SF Symbol
        return image.ImageWithRenderingMode(source.Color is null
            ? UIImageRenderingMode.AlwaysTemplate
            : UIImageRenderingMode.AlwaysOriginal);
    }
}
