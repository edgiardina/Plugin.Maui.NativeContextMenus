using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Plugin.Maui.NativeContextMenus
{
    [ContentProperty(nameof(Items))]
    public class NativeContextMenuToolbarItem : ToolbarItem
    {
        public ObservableCollection<MenuNode> Items { get; } = new();

        public static readonly BindableProperty ItemsSourceProperty =
            BindableProperty.Create(
                nameof(ItemsSource),
                typeof(IEnumerable<MenuNode>),                 // strong type here
                typeof(NativeContextMenuToolbarItem),
                defaultValue: null,
                propertyChanged: OnItemsSourceChanged);

        public IEnumerable<MenuNode>? ItemsSource
        {
            get => (IEnumerable<MenuNode>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly BindableProperty ItemTappedCommandProperty =
            BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(NativeContextMenuToolbarItem));

        public ICommand? ItemTappedCommand
        {
            get => (ICommand?)GetValue(ItemTappedCommandProperty);
            set => SetValue(ItemTappedCommandProperty, value);
        }

        public event EventHandler<MenuNode>? ItemTapped;
        internal void RaiseItemTapped(MenuNode n)
        {
            ItemTapped?.Invoke(this, n);
            ItemTappedCommand?.Execute(n.CommandParameter ?? n);
        }

        internal Action? RequestRebuildHook;
        public void RequestRebuild() => MainThread.BeginInvokeOnMainThread(() => RequestRebuildHook?.Invoke());

        public NativeContextMenuToolbarItem()
        {
            Items.CollectionChanged += Items_CollectionChanged;
        }

        static void OnItemsSourceChanged(BindableObject bindable, object oldVal, object newVal)
        {
            var owner = (NativeContextMenuToolbarItem)bindable;
            owner.ValidateSourcesOrThrow();     // exclusivity + type checks
            owner.RequestRebuild();
        }

        void Items_CollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
        {
            ValidateSourcesOrThrow();           // exclusivity
            RequestRebuild();
        }

        void ValidateSourcesOrThrow()
        {
            // 1) mutual exclusivity
            if (ItemsSource is not null && Items.Count > 0)
                throw new InvalidOperationException(
                    $"{nameof(NativeContextMenuToolbarItem)} cannot use both {nameof(ItemsSource)} and inline {nameof(Items)}. " +
                    $"Set one or the other.");

            // 2) type safety: ItemsSource must be IEnumerable<MenuNode>
            if (ItemsSource is IEnumerable<MenuNode> typed)
            {
                // 3) element validation
                foreach (var node in typed)
                    if (node is null)
                        throw new ArgumentException($"{nameof(ItemsSource)} contains a null {nameof(MenuNode)}.");
            }
            else if (ItemsSource is not null)
            {
                // If someone forces a non-generic binding at runtime, fail loudly.
                throw new ArgumentException(
                    $"{nameof(ItemsSource)} must be an IEnumerable<{nameof(MenuNode)}>.");
            }
        }

        // Helper for handlers
        public IEnumerable<MenuNode> GetActiveRoots() => ItemsSource ?? Items;
    }
}
