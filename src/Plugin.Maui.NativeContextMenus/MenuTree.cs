using System.Collections.Specialized;
using System.Globalization;

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
    /// Changes the check state for a tap on <paramref name="node"/>, then runs its Command and raises Tapped.
    /// </summary>
    public static void Activate(IEnumerable<MenuNode> roots, MenuNode node)
    {
        if (node.Value is not null && node.Parent is MenuNode group)
        {
            // The group sets the check state of its children when SelectedValue changes
            group.SelectedValue = ConvertValue(node.Value, group.SelectedValue);
        }
        else if (node.IsCheckable)
        {
            if (string.IsNullOrEmpty(node.GroupKey))
            {
                node.IsChecked = !node.IsChecked;
            }
            else
            {
                foreach (var other in Descendants(roots))
                    if (other.IsCheckable && other.GroupKey == node.GroupKey)
                        other.IsChecked = other == node;
            }
        }

        if (node.Command?.CanExecute(node.CommandParameter) == true)
            node.Command.Execute(node.CommandParameter);

        node.RaiseTapped();
    }

    /// <summary>
    /// Compares a node Value with a SelectedValue. XAML gives a Value as text, so "Grid" is equal to an enum Grid and "50" to the number 50.
    /// </summary>
    public static bool ValuesEqual(object? value, object? selected) =>
        Equals(value, selected) ||
        (value is not null && selected is not null &&
         string.Equals(ToText(value), ToText(selected), StringComparison.Ordinal));

    /// <summary>
    /// Changes <paramref name="value"/> to the type of the current selection, so a binding to an enum or a number gets that type.
    /// </summary>
    public static object ConvertValue(object value, object? current)
    {
        if (current is null || current.GetType() == value.GetType())
            return value;

        try
        {
            var type = current.GetType();
            return type.IsEnum
                ? Enum.Parse(type, ToText(value))
                : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException)
        {
            return value;
        }
    }

    static string ToText(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// True when all nodes are checkable leaves that share one GroupKey.
    /// </summary>
    public static bool IsRadioGroup(IReadOnlyCollection<MenuNode> nodes) =>
        nodes.Count > 0 &&
        !string.IsNullOrEmpty(nodes.First().GroupKey) &&
        nodes.All(n => n.IsCheckable && n.Children.Count == 0 && n.GroupKey == nodes.First().GroupKey);
}
