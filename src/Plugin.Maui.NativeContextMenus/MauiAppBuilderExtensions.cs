using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// Extension methods for registering the Native Context Menus plugin with MauiAppBuilder
/// Note: Windows is not supported - use built-in .NET MAUI context menus instead
/// </summary>
public static class MauiAppBuilderExtensions
{
	/// <summary>
	/// Configures the Native Context Menus plugin for the MAUI application
	/// Supports Android, iOS, and macOS platforms
	/// </summary>
	/// <param name="builder">The MauiAppBuilder instance</param>
	/// <returns>The MauiAppBuilder instance for method chaining</returns>
	public static MauiAppBuilder UseNativeContextMenus(this MauiAppBuilder builder)
	{
		builder.Services.AddSingleton<INativeContextMenus, NativeContextMenusImplementation>();
		
		// For iOS automatic injection, developers can call the injector manually
		// For Android, developers should use AndroidMenuHelper.InjectToolbarMenuItems()
		// in their Activity's OnCreateOptionsMenu method
		
		return builder;
	}
}