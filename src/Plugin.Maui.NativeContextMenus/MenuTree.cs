using System.Collections.Specialized;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// Shared helpers for MenuNode trees.
/// </summary>
static class MenuTree
{
    /// <summary>
    /// Keeps the logical children of <paramref name="owner"/> equal to <paramref name="nodes"/>.
    /// </summary>
    public static void SyncLogicalChildren(Element owner, IReadOnlyList<MenuNode> nodes, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            owner.ClearLogicalChildren();
            foreach (var node in nodes)
                owner.AddLogicalChild(node);
            return;
        }

        if (e.OldItems is not null)
            foreach (MenuNode node in e.OldItems)
                owner.RemoveLogicalChild(node);

        if (e.NewItems is not null)
            foreach (MenuNode node in e.NewItems)
                owner.AddLogicalChild(node);
    }

    public static IEnumerable<MenuNode> Descendants(IEnumerable<MenuNode> roots)
    {
        foreach (var root in roots)
        {
            yield return root;
            foreach (var child in Descendants(root.Children))
                yield return child;
        }
    }

    /// <summary>
    /// True when all nodes are checkable leaves that share one GroupKey.
    /// </summary>
    public static bool IsRadioGroup(IReadOnlyCollection<MenuNode> nodes) =>
        nodes.Count > 0 &&
        !string.IsNullOrEmpty(nodes.First().GroupKey) &&
        nodes.All(n => n.IsCheckable && n.Children.Count == 0 && n.GroupKey == nodes.First().GroupKey);
}
