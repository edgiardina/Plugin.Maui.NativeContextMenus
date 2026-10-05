namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// The owner of a MenuNode tree. The platform code builds the native menu from it.
/// </summary>
interface IMenuHost
{
    IEnumerable<MenuNode> Roots { get; }

    /// <summary>
    /// Called by the platform code when the user taps a leaf node.
    /// </summary>
    void Activate(MenuNode node);
}
