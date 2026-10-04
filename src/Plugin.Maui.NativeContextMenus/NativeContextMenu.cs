using System.Collections.ObjectModel;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// A native context menu for a view. Attach it with <see cref="NativeContextMenus.ContextMenuProperty"/>.
/// iOS and Mac Catalyst show a UIContextMenuInteraction menu. Android shows a PopupMenu on long press.
/// </summary>
[ContentProperty(nameof(Items))]
public class NativeContextMenu : Element
{
    // Optional header text (iOS and Mac Catalyst only)
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(NativeContextMenu), default(string));

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ObservableCollection<MenuNode> Items { get; } = new();

    /// <summary>
    /// Raised after a leaf node is tapped and its Command has run.
    /// </summary>
    public event EventHandler<MenuNode>? ItemTapped;

    public NativeContextMenu()
    {
        Items.CollectionChanged += (_, e) => MenuTree.SyncLogicalChildren(this, Items, e);
    }

    internal bool HasVisibleItems => Items.Any(n => n.IsVisible);

    /// <summary>
    /// Called by the platform code when the user taps a leaf node.
    /// </summary>
    internal void Activate(MenuNode node)
    {
        if (node.IsCheckable)
        {
            if (string.IsNullOrEmpty(node.GroupKey))
            {
                node.IsChecked = !node.IsChecked;
            }
            else
            {
                foreach (var other in MenuTree.Descendants(Items))
                    if (other.IsCheckable && other.GroupKey == node.GroupKey)
                        other.IsChecked = other == node;
            }
        }

        if (node.Command?.CanExecute(node.CommandParameter) == true)
            node.Command.Execute(node.CommandParameter);

        node.RaiseTapped();
        ItemTapped?.Invoke(this, node);
    }
}
