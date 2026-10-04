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
        if (platformView is not UIView view || attachments.TryGetValue(view, out _))
            return;

        var attachment = new Attachment(element);
        view.UserInteractionEnabled = true;
        view.AddInteraction(attachment.Interaction);
        attachments.Add(view, attachment);
    }

    static partial void PlatformDetach(object? platformView)
    {
        if (platformView is not UIView view || !attachments.TryGetValue(view, out var attachment))
            return;

        view.RemoveInteraction(attachment.Interaction);
        attachments.Remove(view);
    }

    sealed class Attachment
    {
        readonly InteractionDelegate interactionDelegate;

        public UIContextMenuInteraction Interaction { get; }

        public Attachment(VisualElement element)
        {
            interactionDelegate = new InteractionDelegate(element);
            Interaction = new UIContextMenuInteraction(interactionDelegate);
        }
    }

    sealed class InteractionDelegate : UIContextMenuInteractionDelegate
    {
        readonly WeakReference<VisualElement> element;

        public InteractionDelegate(VisualElement element) =>
            this.element = new WeakReference<VisualElement>(element);

        public override UIContextMenuConfiguration? GetConfigurationForMenu(UIContextMenuInteraction interaction, CGPoint location)
        {
            if (!element.TryGetTarget(out var target) ||
                GetContextMenu(target) is not { HasVisibleItems: true } menu)
                return null;

            var mauiContext = target.Handler?.MauiContext;

            // The menu is built each time it opens, so it always shows the current node state
            return UIContextMenuConfiguration.Create(
                identifier: null,
                previewProvider: null,
                actionProvider: _ => UIMenu.Create(menu.Title ?? string.Empty, BuildElements(menu.Items, menu, mauiContext)));
        }
    }

    static UIMenuElement[] BuildElements(IEnumerable<MenuNode> nodes, NativeContextMenu owner, IMauiContext? mauiContext) =>
        nodes.Where(n => n.IsVisible).Select(n => BuildElement(n, owner, mauiContext)).ToArray();

    static UIMenuElement BuildElement(MenuNode node, NativeContextMenu owner, IMauiContext? mauiContext)
    {
        var title = node.Title ?? string.Empty;
        var image = ToUIImage(node.Icon, mauiContext);

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
        if (node.KeepMenuOpen && (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16)))
            attributes |= UIMenuElementAttributes.KeepsMenuPresented;
        action.Attributes = attributes;

        action.State = node.IsCheckable && node.IsChecked ? UIMenuElementState.On : UIMenuElementState.Off;
        return action;
    }

    static UIImage? ToUIImage(ImageSource? source, IMauiContext? mauiContext) => source switch
    {
        // A file name that is not in the app bundle is used as an SF Symbol name
        FileImageSource { File: { Length: > 0 } file } => UIImage.FromBundle(file) ?? UIImage.GetSystemImage(file),
        FontImageSource { Glyph: { Length: > 0 } } font when mauiContext is not null => RenderGlyph(font, mauiContext),
        _ => null,
    };

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
