# Plugin.Maui.NativeContextMenus

[![NuGet](https://img.shields.io/nuget/v/Plugin.Maui.NativeContextMenus.svg)](https://www.nuget.org/packages/Plugin.Maui.NativeContextMenus/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Plugin.Maui.NativeContextMenus.svg)](https://www.nuget.org/packages/Plugin.Maui.NativeContextMenus/)

`Plugin.Maui.NativeContextMenus` provides the ability to create native context menus in your .NET MAUI application.

## Features

- **Cross-platform**: Works on Android, iOS, macOS, and Windows
- **Native UI**: Uses each platform's native context menu implementation
- **Easy Integration**: Simple API that integrates seamlessly with .NET MAUI
- **Customizable**: Support for various menu item types and configurations

## Supported Platforms

| Platform | Minimum Version Supported |
| -------- | ------------------------- |
| iOS      | 14.2+                     |
| macOS    | 14.0+                     |
| Android  | API 21 (Android 5.0)+     |
| Windows  | 10.0.17763.0+             |

## Installation

`Plugin.Maui.NativeContextMenus` is available on NuGet. Add it to your project using:

```xml
<PackageReference Include="Plugin.Maui.NativeContextMenus" Version="1.0.0" />
```

Or via the Package Manager Console:

```powershell
Install-Package Plugin.Maui.NativeContextMenus
```

## Getting Started

### 1. Register the Plugin

In your `MauiProgram.cs` file, add the plugin to your MAUI app:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseNativeContextMenus(); // Add this line
            
        return builder.Build();
    }
}
```

### 2. Basic Usage

Add context menus to your views:

```csharp
// In your page or view
var label = new Label { Text = "Right-click me!" };

var contextMenu = new NativeContextMenu();
contextMenu.MenuItems.Add(new MenuNode 
{ 
    Text = "Copy", 
    Command = new Command(() => /* Handle copy */) 
});
contextMenu.MenuItems.Add(new MenuNode 
{ 
    Text = "Paste", 
    Command = new Command(() => /* Handle paste */) 
});

NativeContextMenus.SetContextMenu(label, contextMenu);
```

### 3. XAML Usage

You can also define context menus in XAML:

```xml
<ContentPage xmlns:cm="clr-namespace:Plugin.Maui.NativeContextMenus;assembly=Plugin.Maui.NativeContextMenus">
    <Label Text="Right-click me!">
        <cm:NativeContextMenus.ContextMenu>
            <cm:NativeContextMenu>
                <cm:MenuNode Text="Copy" Command="{Binding CopyCommand}" />
                <cm:MenuNode Text="Paste" Command="{Binding PasteCommand}" />
                <cm:MenuNode Text="-" /> <!-- Separator -->
                <cm:MenuNode Text="Delete" Command="{Binding DeleteCommand}" />
            </cm:NativeContextMenu>
        </cm:NativeContextMenus.ContextMenu>
    </Label>
</ContentPage>
```

## Advanced Features

### Menu Item Types

- **Standard Items**: Regular menu items with text and commands
- **Separators**: Visual dividers between menu groups
- **Submenus**: Nested menu structures
- **Icons**: Platform-specific icon support

### Conditional Menu Items

```csharp
var menuItem = new MenuNode
{
    Text = "Conditional Item",
    Command = myCommand,
    IsVisible = someCondition,
    IsEnabled = anotherCondition
};
```

## Sample App

Check out the [sample application](samples/) to see the plugin in action and learn about all the available features.

## API Reference

### NativeContextMenu

The main class for creating context menus.

**Properties:**
- `MenuItems`: Collection of `MenuNode` items

### MenuNode

Represents a single menu item.

**Properties:**
- `Text`: The display text for the menu item
- `Command`: The command to execute when the item is selected
- `CommandParameter`: Optional parameter for the command
- `IsVisible`: Whether the item should be visible
- `IsEnabled`: Whether the item should be enabled
- `Icon`: Platform-specific icon (optional)
- `Children`: Child menu items for submenus

### NativeContextMenus (Static Class)

**Methods:**
- `SetContextMenu(BindableObject, NativeContextMenu)`: Attaches a context menu to a view
- `GetContextMenu(BindableObject)`: Retrieves the context menu from a view

## Platform Implementation Details

### iOS/macOS
Uses `UIContextMenuConfiguration` and `NSMenu` respectively for native context menu support.

### Android
Implements context menus using `PopupMenu` and `ContextMenu` APIs.

### Windows
Utilizes `MenuFlyout` for WinUI context menu functionality.

## Contributing

Contributions are welcome! Please read our [contributing guidelines](CONTRIBUTING.md) before submitting pull requests.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Thanks to the .NET MAUI team for providing the framework
- Inspired by native context menu implementations across platforms

## Support

If you encounter any issues or have questions:

1. Check the [sample app](samples/) for usage examples
2. Browse the [issues](https://github.com/jfversluis/Plugin.Maui.NativeContextMenus/issues) on GitHub
3. Create a new issue if you can't find an existing solution

---

Made with ❤️ for the .NET MAUI community