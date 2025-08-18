# NativeContextMenuToolbarItem Usage Guide

## Overview

The `NativeContextMenuToolbarItem` provides cross-platform hierarchical toolbar menus with instant-apply behavior. It supports both declarative XAML (inline items) and MVVM binding (`ItemsSource`) with mutual exclusivity and strong typing.

## Setup

### 1. Register the Plugin

In your `MauiProgram.cs`:

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

Add to your page's XAML:

```xml
<ContentPage.ToolbarItems>
    <local:NativeContextMenuToolbarItem Text="Menu" ItemTappedCommand="{Binding HandleMenuCommand}">
        <local:MenuNode Title="Action 1" Command="{Binding Command1}" />
        <local:MenuNode Title="Submenu">
            <local:MenuNode Title="Sub Action 1" Command="{Binding SubCommand1}" />
            <local:MenuNode Title="Sub Action 2" Command="{Binding SubCommand2}" />
        </local:MenuNode>
    </local:NativeContextMenuToolbarItem>
</ContentPage.ToolbarItems>
```

## MenuNode Properties

| Property | Type | Description |
|----------|------|-------------|
| `Title` | `string?` | Display text for the menu item |
| `Icon` | `ImageSource?` | Optional icon (File/Font/Stream supported) |
| `IsEnabled` | `bool` | Enable/disable state (default: true) |
| `Destructive` | `bool` | Shows as destructive action (red on iOS) |
| `KeepMenuOpen` | `bool` | iOS: keeps menu open after tap |
| `IsCheckable` | `bool` | Shows checkmark/toggle |
| `IsChecked` | `bool` | Checked state (used with IsCheckable) |
| `GroupKey` | `string?` | Radio group identifier for single-selection |
| `Command` | `ICommand?` | Command executed when tapped |
| `CommandParameter` | `object?` | Optional command parameter |
| `Children` | `ObservableCollection<MenuNode>` | Child nodes for submenus |

## Usage Patterns

### Inline XAML (Declarative)

```xml
<local:NativeContextMenuToolbarItem Text="Filters" ItemTappedCommand="{Binding ApplyFilterCommand}">
    
    <!-- Radio group example -->
    <local:MenuNode Title="Layout">
        <local:MenuNode Title="List" Icon="list.png" GroupKey="layout" IsCheckable="True" IsChecked="True" CommandParameter="list" />
        <local:MenuNode Title="Grid" Icon="grid.png" GroupKey="layout" IsCheckable="True" CommandParameter="grid" />
    </local:MenuNode>

    <!-- Another radio group -->
    <local:MenuNode Title="Ranking Type: WPPR">
        <local:MenuNode Title="WPPR" GroupKey="rank" IsCheckable="True" IsChecked="True" CommandParameter="WPPR" />
        <local:MenuNode Title="Pro" GroupKey="rank" IsCheckable="True" CommandParameter="Pro" />
        <local:MenuNode Title="Women" GroupKey="rank" IsCheckable="True" CommandParameter="Women" />
    </local:MenuNode>

</local:NativeContextMenuToolbarItem>
```

### MVVM Binding (ItemsSource)

```xml
<local:NativeContextMenuToolbarItem Text="Filters"
                                   ItemsSource="{Binding FilterMenus}"
                                   ItemTappedCommand="{Binding ApplyFilterCommand}" />
```

```csharp
// ViewModel
public ObservableCollection<MenuNode> FilterMenus { get; } = new()
{
    new MenuNode 
    { 
        Title = $"Ranking Type: {State.RankingType}", 
        Children = 
        {
            new MenuNode { Title = "WPPR", GroupKey = "rank", IsCheckable = true, IsChecked = true, CommandParameter = "WPPR" },
            new MenuNode { Title = "Pro", GroupKey = "rank", IsCheckable = true, CommandParameter = "Pro" },
            // ...
        } 
    },
    // ...
};
```

## Platform-Specific Integration

### iOS/macOS (Automatic)

Works automatically with iOS 14+ UIMenu support. For iOS 13, falls back to first action.

Features:
- ? Hierarchical menus with UIMenu/UIAction
- ? Radio groups with UIMenuOptions.SingleSelection
- ? Checkmarks and destructive styling
- ? Icon support (File and Font image sources)
- ? Inline display for thumbnail rows

### Android (Manual Integration Required)

Due to Android's complex Activity lifecycle, manual integration is recommended:

```csharp
public class MainActivity : MauiAppCompatActivity
{
    public override bool OnCreateOptionsMenu(IMenu menu)
    {
        // Let MAUI create the base menu
        base.OnCreateOptionsMenu(menu);
        
        // Inject our custom menu items
        var currentPage = GetCurrentPage(); // Your method to get current page
        if (currentPage != null && AndroidMenuHelper.HasMenuToolbarItems(currentPage))
        {
            AndroidMenuHelper.InjectToolbarMenuItems(menu, currentPage);
        }
        
        return true;
    }
    
    private ContentPage? GetCurrentPage()
    {
        // Implementation depends on your navigation structure
        if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage navPage)
        {
            return navPage.CurrentPage as ContentPage;
        }
        return Microsoft.Maui.Controls.Application.Current?.MainPage as ContentPage;
    }
}
```

Features:
- ? Hierarchical menus with SubMenu
- ? Radio groups with SetGroupCheckable
- ? Checkmarks and enabled/disabled states
- ?? Limited icon support (drawable resources only)

## Behavior Details

### Radio Groups
- Nodes with the same `GroupKey` and `IsCheckable=true` form a radio group
- Only one item per group can be checked at a time
- Checking one item automatically unchecks others in the same group

### Event Flow
1. User taps menu item
2. Radio group logic applies (if applicable)
3. Node's `Command` executes
4. Node's `Tapped` event fires
5. Toolbar's `ItemTappedCommand` executes
6. Menu rebuilds to reflect new state

### Error Handling
- Throws `InvalidOperationException` if both `Items` and `ItemsSource` are populated
- Throws `ArgumentException` if `ItemsSource` is not `IEnumerable<MenuNode>` or contains nulls
- Empty menus show disabled "No items" placeholder

## Examples

See `MenuToolbarSamplePage.cs` and `MenuToolbarSamplePage.xaml` for complete working examples demonstrating both usage patterns with radio groups, checkmarks, and dynamic title updates.

## Troubleshooting

### iOS Issues
- Menus not appearing: Ensure iOS 14+ or check fallback behavior
- Images not loading: Verify bundle resources and file names

### Android Issues  
- Menus not appearing: Implement manual integration with `AndroidMenuHelper`
- Icons missing: Ensure drawable resources exist in Android project

### General Issues
- Items/ItemsSource conflicts: Use only one data source approach
- Radio groups not working: Ensure same `GroupKey` and `IsCheckable=true`
- Commands not executing: Check `CanExecute` implementation