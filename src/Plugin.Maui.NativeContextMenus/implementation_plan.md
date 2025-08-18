# IMPLEMENTATION_PLAN.md

**Goal:** Add a reusable, cross-platform **MenuToolbarItem** to your MAUI plugin that renders hierarchical menus from a tree of items with *instant-apply* behavior. Support **either** declarative XAML (inline items) **or** MVVM binding (`ItemsSource`). Enforce mutual exclusivity and strong typing.  
**Targets:** iOS (`UIMenu`), Android (`Toolbar` `SubMenu`), WinUI (`MenuFlyout`).

---

## 1) Public Surface

### Types (namespace: `MyLib.Controls`)
- `MenuToolbarItem : ToolbarItem`
- `MenuNode : BindableObject` (XAML-friendly)

### Features
- **Dual data source** (choose one):
  - `Items : ObservableCollection<MenuNode>` (inline XAML children)
  - `ItemsSource : IEnumerable<MenuNode>` (binding, *must* be of `MenuNode`)
- **Mutual exclusivity**: throw `InvalidOperationException` if both `ItemsSource` and `Items` are set.
- **Type safety**: throw `ArgumentException` if `ItemsSource` is not `IEnumerable<MenuNode>` or contains `null`.
- **Per-node properties**:
  - `Title : string?`
  - `Icon : ImageSource?` (File/Font/Stream supported by platform converter)
  - `IsEnabled : bool` (default `true`)
  - `Destructive : bool`
  - `KeepMenuOpen : bool` (iOS `KeepsMenuPresented`)
  - `IsCheckable : bool`
  - `IsChecked : bool`
  - `GroupKey : string?` (single-select “radio” behavior per group)
  - `Command : ICommand?`
  - `CommandParameter : object?`
  - `Children : ObservableCollection<MenuNode>`
- **Events/commands**:
  - `MenuToolbarItem.ItemTappedCommand : ICommand?` (executes after the leaf node’s `Command`)
  - Internal `MenuNode.Tapped` event (optional zero-binding hook; raised by handlers)

**Behavior:** Leaf tap → update radio checks (if any) → execute node `Command` → execute toolbar `ItemTappedCommand` → rebuild native menu so titles/checkmarks reflect new state. No “Apply/Reset”; changes apply immediately.

---

## 2) Files to Add (relative to plugin src)

```
src/YourPlugin/Controls/MenuNode.cs
src/YourPlugin/Controls/MenuToolbarItem.cs
src/YourPlugin/Platforms/iOS/MenuToolbarInjector.cs
src/YourPlugin/Platforms/Android/MenuToolbarInjector.cs
src/YourPlugin/Platforms/Windows/MenuToolbarInjector.cs    (optional, WinUI mapping)
src/YourPlugin/Platforms/iOS/ImageSourceExtensions.cs       (ImageSource → UIImage)
```

### A) `Controls/MenuNode.cs` (BindableObject version, XAML-friendly)
- Implements all bindable properties listed above.
- `[ContentProperty(nameof(Children))]` so nodes can nest in XAML.
- Expose `Children : ObservableCollection<MenuNode>`.
- Optional `Tapped` event (raised by handlers).

### B) `Controls/MenuToolbarItem.cs`
- `[ContentProperty(nameof(Items))]` so you can write nodes inline.
- `Items : ObservableCollection<MenuNode>` (inline XAML path).
- `ItemsSource : IEnumerable<MenuNode>?` (binding path) with validation:
  - If `ItemsSource != null` and `Items.Count > 0` ⇒ throw `InvalidOperationException`.
  - If `ItemsSource` is not `IEnumerable<MenuNode>` ⇒ throw `ArgumentException`.
  - If any `MenuNode` is `null` ⇒ throw `ArgumentException`.
- `ItemTappedCommand : ICommand?`.
- `RequestRebuildHook : Action?` set by platform injection to refresh menus.
- `RequestRebuild()` helper (invokes hook on UI thread).
- `GetActiveRoots()` ⇒ returns `ItemsSource ?? Items`.

---

## 3) Platform Integration

> `ToolbarItem` has **no** MAUI handler. You inject menus where MAUI maps a `Page`’s `ToolbarItems` into native controls.

### iOS (UINavigationItem + UIMenu)
- Where the platform renderer/handler assigns `UIBarButtonItem`s, replace any `MenuToolbarItem` with a `UIBarButtonItem` configured as:
  - `ShowsMenuAsPrimaryAction = true`
  - `Menu = BuildMenu(menuToolbarItem)`
- Build menus recursively from `MenuNode`:
  - If node has **children**: create `UIMenu`.
    - If all children share `GroupKey` and are `IsCheckable`, use `UIMenuOptions.SingleSelection`.
    - For the first top-level group (optional), add `DisplayInline` to mimic iOS inline rows.
  - If node is a **leaf**: create `UIAction`.
    - Map `IsEnabled`, `Destructive`, `KeepMenuOpen`.
    - Map `IsCheckable/IsChecked` → `UIAction.State`.
    - On invoke: apply radio checks, execute `node.Command`, raise `node.Tapped`, then `owner.ItemTappedCommand`, then `owner.RequestRebuild()`.
- Keep a reference from `MenuToolbarItem` to its `UIBarButtonItem` via `RequestRebuildHook` so you can rebuild `bar.Menu` when state changes.
- **Icons:** convert `ImageSource` → `UIImage` (support `FileImageSource`, `FontImageSource`; cache for perf).

### Android (Toolbar overflow + SubMenu)
- Post-process the `Toolbar.Menu` (or override the Page mapping) and for each `MenuToolbarItem` add a **`SubMenu`** with the tree.
- Radio behavior:
  - If sibling leaf nodes are `IsCheckable` and share `GroupKey`, set `SetGroupCheckable(groupId, true, true)` using a stable `groupId` (e.g., `GroupKey.GetHashCode()`).
- Leaf click: apply checks, execute commands, then `owner.RequestRebuild()` to refresh titles/checked states.
- Note: `PopupMenu` has no submenus; prefer `Toolbar` overflow for hierarchical menus.

---

## 4) Wiring (template-friendly)

- **Option A (clean):** Create a small “injector” called after MAUI sets toolbar items:
  - iOS: obtain the page’s `UINavigationItem` and call `MenuToolbarInjector.ApplyTo(...)`.
  - Android: given a `Toolbar`/`IMenu`, call `MenuToolbarInjector.ApplyTo(menu, page.ToolbarItems)`.
  - Windows: attach a `MenuFlyout` to the `AppBarButton` for your `MenuToolbarItem`.
- **Option B (custom handler):** Derive `PageHandler` and override the method that syncs `ToolbarItems` → native. Replace items that are `MenuToolbarItem` with your menu-enabled native items, calling the same builder functions above.

> In both cases, after any change to nodes (title/checked/etc.), call `menuToolbarItem.RequestRebuild()` (your injector re-creates native menus via the stored hook).

---

## 5) Usage Examples

### A) Declarative XAML (inline)
```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:mylib="clr-namespace:MyLib.Controls"
    x:Class="Ifpa.Views.SomePage">

  <ContentPage.ToolbarItems>
    <mylib:MenuToolbarItem Text="Filters" ItemTappedCommand="{Binding ApplyFilterCommand}">
      <mylib:MenuNode Title="Layout">
        <mylib:MenuNode Title="" Icon="list.png" GroupKey="layout" IsCheckable="True" IsChecked="True" CommandParameter="list" />
        <mylib:MenuNode Title="" Icon="grid.png" GroupKey="layout" IsCheckable="True" CommandParameter="grid" />
      </mylib:MenuNode>

      <mylib:MenuNode Title="Ranking Type: WPPR">
        <mylib:MenuNode Title="WPPR"  GroupKey="rank" IsCheckable="True" IsChecked="True" CommandParameter="WPPR" />
        <mylib:MenuNode Title="Pro"   GroupKey="rank" IsCheckable="True" CommandParameter="Pro" />
        <mylib:MenuNode Title="Women" GroupKey="rank" IsCheckable="True" CommandParameter="Women" />
      </mylib:MenuNode>

      <mylib:MenuNode Title="Players: 100">
        <mylib:MenuNode Title="25"  GroupKey="players" IsCheckable="True" CommandParameter="25" />
        <mylib:MenuNode Title="50"  GroupKey="players" IsCheckable="True" CommandParameter="50" />
        <mylib:MenuNode Title="100" GroupKey="players" IsCheckable="True" IsChecked="True" CommandParameter="100" />
        <mylib:MenuNode Title="Custom…" />
      </mylib:MenuNode>
    </mylib:MenuToolbarItem>
  </ContentPage.ToolbarItems>
</ContentPage>
```

### B) Binding (MVVM)
```xml
<mylib:MenuToolbarItem Text="Filters"
                       ItemsSource="{Binding FilterMenus}"
                       ItemTappedCommand="{Binding ApplyFilterCommand}" />
```

```csharp
// VM
public ObservableCollection<MenuNode> FilterMenus { get; } = new()
{
    new MenuNode { Title = $"Ranking Type: {State.RankingType}", Children = { /* … */ } },
    new MenuNode { Title = $"Players: {State.Players}", Children = { /* … */ } }
};
```

---

## 6) Error Handling & Guardrails

- Throw on **both** `Items` and `ItemsSource` being populated.
- Throw if `ItemsSource` is not `IEnumerable<MenuNode>` or contains `null` entries.
- In builders, assert `GetActiveRoots()` is non-null or return an empty menu with a disabled placeholder.
- On iOS, ensure image conversion is on **main thread**; consider caching `UIImage` for `FileImageSource`/`FontImageSource`.

---

## 7) Notes & Gotchas

- iOS inline “row of thumbnails”: put that submenu **first** and add `DisplayInline` to the `UIMenu` options.
- Android OEMs may hide icons in overflow; keep titles meaningful.
- Keep rebuilds light: avoid heavy allocations; cache converted icons.
- Accessibility: provide non-empty `Title` or `accessibilityLabel` equivalents for icon-only actions.
- If you need multi-change without dismiss on iOS, set `KeepMenuOpen=true` for specific actions.

---

## 8) Done Criteria

- XAML sample page demonstrates both inline and ItemsSource usage.
- iOS: tapping options updates checkmarks and top-level titles immediately.
- Android: submenu radio groups enforce exclusivity; titles refresh on selection.
- (Optional) Windows mapping works with `MenuFlyout` radiobutton items.
- Exceptions thrown for bad configuration (mixed mode, wrong type).

---

## 9) Next Steps for Copilot/Claude

1. **Add the files** listed in §2 with the skeletons you already have (use prior code from the conversation for full class bodies).
2. **Implement injectors** per §3; wire them where your template constructs platform toolbars.
3. **Create a sample page** using both XAML approaches in §5.
4. **Test iOS first**, then Android (radio groups, title refresh), then Windows if needed.
5. Add basic unit tests for validation logic (`Items` vs `ItemsSource`, type checks).
