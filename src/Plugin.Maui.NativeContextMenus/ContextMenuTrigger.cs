namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// The gesture that opens a <see cref="NativeContextMenu"/>.
/// </summary>
public enum ContextMenuTrigger
{
    /// <summary>
    /// Long press (secondary click on Mac Catalyst). iOS shows a preview of the view.
    /// </summary>
    LongPress,

    /// <summary>
    /// Tap. The menu opens adjacent to the view.
    /// </summary>
    Tap,
}
