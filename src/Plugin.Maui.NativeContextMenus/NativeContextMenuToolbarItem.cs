using System.Collections.ObjectModel;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// A toolbar item that opens a native menu on a tap.
/// iOS and Mac Catalyst show the UIMenu of the bar button. Android shows a PopupMenu anchored to the toolbar button.
/// </summary>
[ContentProperty(nameof(Items))]
public partial class NativeContextMenuToolbarItem : ToolbarItem, IMenuHost
{
    public ObservableCollection<MenuNode> Items { get; } = new();

    // Nodes made in code. When set, the menu shows these nodes and not Items.
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable<MenuNode>), typeof(NativeContextMenuToolbarItem), default(IEnumerable<MenuNode>));

    public IEnumerable<MenuNode>? ItemsSource
    {
        get => (IEnumerable<MenuNode>?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Raised after a leaf node is tapped and its Command has run.
    /// </summary>
    public event EventHandler<MenuNode>? ItemTapped;

    public NativeContextMenuToolbarItem()
    {
        // Items are logical children so BindingContext and RelativeSource bindings reach them
        Items.CollectionChanged += (_, e) => MenuTree.SyncLogicalChildren(this, Items, e);
    }

    IEnumerable<MenuNode> IMenuHost.Roots => ItemsSource ?? Items;

    void IMenuHost.Activate(MenuNode node)
    {
        MenuTree.Activate(((IMenuHost)this).Roots, node);
        ItemTapped?.Invoke(this, node);
    }

    // The page that shows this item. The item can be in the ToolbarItems of a container page.
    internal Page? FindPage()
    {
        var page = Parent as Page;
        while (true)
        {
            var current = page switch
            {
                Shell shell => shell.CurrentPage,
                NavigationPage navigation => navigation.CurrentPage,
                TabbedPage tabbed => tabbed.CurrentPage,
                _ => null,
            };

            if (current is null)
                return page;
            page = current;
        }
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        PlatformParentSet();
    }

    protected override void OnClicked()
    {
        base.OnClicked();
        PlatformClicked();
    }

    partial void PlatformParentSet();

    partial void PlatformClicked();
}
