using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Plugin.Maui.NativeContextMenus
{
    /// <summary>
    /// XAML-friendly menu tree node for NativeContextMenu and NativeContextMenuToolbarItem.
    /// All properties are bindable; use Children to build submenus.
    /// A node with Children and no Title is an inline section (a divided group).
    /// </summary>
    [ContentProperty(nameof(Children))]
    public class MenuNode : Element
    {
        // Title (e.g., "Ranking Type: WPPR" or "WPPR")
        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(MenuNode), default(string));

        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Optional icon (FileImageSource / FontImageSource / StreamImageSource)
        public static readonly BindableProperty IconProperty =
            BindableProperty.Create(nameof(Icon), typeof(ImageSource), typeof(MenuNode), default(ImageSource));

        public ImageSource? Icon
        {
            get => (ImageSource?)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        // System icon with autocomplete (iOS and Mac Catalyst). Icon has priority if it is set.
        public static readonly BindableProperty SystemIconProperty =
            BindableProperty.Create(nameof(SystemIcon), typeof(SystemIcon), typeof(MenuNode), SystemIcon.None);

        public SystemIcon SystemIcon
        {
            get => (SystemIcon)GetValue(SystemIconProperty);
            set => SetValue(SystemIconProperty, value);
        }

        // SF Symbol name, for a symbol that SystemIcon does not have (iOS and Mac Catalyst)
        public static readonly BindableProperty SymbolNameProperty =
            BindableProperty.Create(nameof(SymbolName), typeof(string), typeof(MenuNode), default(string));

        public string? SymbolName
        {
            get => (string?)GetValue(SymbolNameProperty);
            set => SetValue(SymbolNameProperty, value);
        }

        // Enabled/disabled state
        public static readonly BindableProperty IsEnabledProperty =
            BindableProperty.Create(nameof(IsEnabled), typeof(bool), typeof(MenuNode), true);

        public bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        // Hidden nodes are left out of the native menu
        public static readonly BindableProperty IsVisibleProperty =
            BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(MenuNode), true);

        public bool IsVisible
        {
            get => (bool)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        // Destructive (maps to red style on iOS, etc.)
        public static readonly BindableProperty DestructiveProperty =
            BindableProperty.Create(nameof(Destructive), typeof(bool), typeof(MenuNode), false);

        public bool Destructive
        {
            get => (bool)GetValue(DestructiveProperty);
            set => SetValue(DestructiveProperty, value);
        }

        // KeepsMenuOpen (iOS: UIMenuElementAttributes.KeepsMenuPresented)
        public static readonly BindableProperty KeepMenuOpenProperty =
            BindableProperty.Create(nameof(KeepMenuOpen), typeof(bool), typeof(MenuNode), false);

        public bool KeepMenuOpen
        {
            get => (bool)GetValue(KeepMenuOpenProperty);
            set => SetValue(KeepMenuOpenProperty, value);
        }

        // Checkable (shows checkmark/toggle)
        public static readonly BindableProperty IsCheckableProperty =
            BindableProperty.Create(nameof(IsCheckable), typeof(bool), typeof(MenuNode), false);

        public bool IsCheckable
        {
            get => (bool)GetValue(IsCheckableProperty);
            set => SetValue(IsCheckableProperty, value);
        }

        // Checked state (used with IsCheckable)
        public static readonly BindableProperty IsCheckedProperty =
            BindableProperty.Create(nameof(IsChecked), typeof(bool), typeof(MenuNode), false, BindingMode.TwoWay);

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        // GroupKey: define radio groups (single-select per group)
        public static readonly BindableProperty GroupKeyProperty =
            BindableProperty.Create(nameof(GroupKey), typeof(string), typeof(MenuNode), default(string));

        public string? GroupKey
        {
            get => (string?)GetValue(GroupKeyProperty);
            set => SetValue(GroupKeyProperty, value);
        }

        // The value of this node in a radio group. The parent node holds the selection in SelectedValue.
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(object), typeof(MenuNode), default(object),
                propertyChanged: (bindable, _, _) => (((MenuNode)bindable).Parent as MenuNode)?.SyncSelection());

        public object? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        // The Value of the checked child. The children that have a Value are a radio group.
        public static readonly BindableProperty SelectedValueProperty =
            BindableProperty.Create(nameof(SelectedValue), typeof(object), typeof(MenuNode), default(object), BindingMode.TwoWay,
                propertyChanged: (bindable, _, _) => ((MenuNode)bindable).SyncSelection());

        public object? SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        // Command executed when this node is tapped (before ItemTappedCommand on owner)
        public static readonly BindableProperty CommandProperty =
            BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(MenuNode), default(ICommand));

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        // Optional CommandParameter
        public static readonly BindableProperty CommandParameterProperty =
            BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(MenuNode), default(object));

        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Child nodes (submenu contents). Empty = leaf action.
        /// </summary>
        public ObservableCollection<MenuNode> Children { get; } = new();

        public MenuNode()
        {
            // Children are logical children so BindingContext and RelativeSource bindings reach them
            Children.CollectionChanged += (_, e) =>
            {
                MenuTree.SyncLogicalChildren(this, Children, e);
                SyncSelection();
            };
        }

        string? selectionGroupKey;

        // Sets the check state of the children that have a Value. The platform code reads only the check state.
        void SyncSelection()
        {
            foreach (var child in Children)
            {
                if (child.Value is null)
                    continue;

                child.IsCheckable = true;
                child.GroupKey = selectionGroupKey ??= $"SelectedValue:{Guid.NewGuid():N}";
                child.IsChecked = MenuTree.ValuesEqual(child.Value, SelectedValue);
            }
        }

        internal bool IsSection => Children.Count > 0 && string.IsNullOrEmpty(Title);

        /// <summary>
        /// Optional code-behind event for zero-binding scenarios.
        /// Platform handlers should raise this alongside Command.
        /// </summary>
        public event EventHandler? Tapped;
        internal void RaiseTapped() => Tapped?.Invoke(this, EventArgs.Empty);
    }

}
