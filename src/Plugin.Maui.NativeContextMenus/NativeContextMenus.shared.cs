namespace Plugin.Maui.NativeContextMenus;

public static class NativeContextMenus
{
	static INativeContextMenus? defaultImplementation;

	/// <summary>
	/// Provides the default implementation for static usage of this API.
	/// </summary>
	public static INativeContextMenus Default =>
		defaultImplementation ??= new NativeContextMenusImplementation();

	internal static void SetDefault(INativeContextMenus? implementation) =>
		defaultImplementation = implementation;
}
