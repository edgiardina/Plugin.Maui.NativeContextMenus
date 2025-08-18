using Microsoft.Extensions.DependencyInjection;

namespace Plugin.Maui.NativeContextMenus;

/// <summary>
/// Extension methods for registering the Native Context Menus plugin with MauiAppBuilder
/// </summary>
public static class MauiAppBuilderExtensions
{
	/// <summary>
	/// Configures the Native Context Menus plugin for the MAUI application
	/// </summary>
	/// <param name="builder">The MauiAppBuilder instance</param>
	/// <returns>The MauiAppBuilder instance for method chaining</returns>
	public static MauiAppBuilder UseNativeContextMenus(this MauiAppBuilder builder)
	{
		builder.Services.AddSingleton<INativeContextMenus, NativeContextMenusImplementation>();
		
		return builder;
	}
}