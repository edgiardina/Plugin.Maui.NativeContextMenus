using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Plugin.Maui.NativeContextMenus;

namespace Plugin.Maui.NativeContextMenus.Sample;

public class MainPageViewModel : INotifyPropertyChanged
{
    private string _currentViewMode = "List";
    private string _currentSortMode = "Name";
    private int _itemCount = 10;
    private string _statusText = "Ready";
    private Density _density = Density.Regular;
    private int _pageSize = 50;

    public MainPageViewModel()
    {
        // Initialize commands
        RefreshCommand = new Command(OnRefresh);
        SetViewCommand = new Command<string>(OnSetView);
        SetSortCommand = new Command<string>(OnSetSort);
        ClearCommand = new Command(OnClear);
        AddItemCommand = new Command(OnAddItem);
        ApplyFilterCommand = new Command(() =>
        {
            System.Diagnostics.Debug.WriteLine("ApplyFilterCommand executed - this is a fallback command");
            StatusText = "Filter menu fallback command executed";
            Task.Delay(2000).ContinueWith(_ => StatusText = "Ready");
        });

        // Initialize menu items for MVVM binding
        InitializeMenuItems();
    }

    #region Properties

    public string CurrentViewMode
    {
        get => _currentViewMode;
        set => SetProperty(ref _currentViewMode, value);
    }

    public string CurrentSortMode
    {
        get => _currentSortMode;
        set => SetProperty(ref _currentSortMode, value);
    }

    public int ItemCount
    {
        get => _itemCount;
        set
        {
            if (SetProperty(ref _itemCount, value))
                OnPropertyChanged(nameof(HasItems));
        }
    }

    public bool HasItems => ItemCount > 0;

    public Density Density
    {
        get => _density;
        set => SetProperty(ref _density, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ObservableCollection<MenuNode> MenuItems { get; } = new();

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }
    public ICommand SetViewCommand { get; }
    public ICommand SetSortCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand ApplyFilterCommand { get; }

    #endregion

    #region Command Handlers

    private void OnRefresh()
    {
        System.Diagnostics.Debug.WriteLine("OnRefresh command executed");
        StatusText = "Refreshing...";
        // Simulate async work
        Task.Delay(1000).ContinueWith(_ =>
        {
            StatusText = "Refreshed!";
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Task.Delay(2000).ContinueWith(__ => StatusText = "Ready");
            });
        });
    }

    private void OnSetView(string viewMode)
    {
        System.Diagnostics.Debug.WriteLine($"OnSetView command executed with: {viewMode}");
        CurrentViewMode = viewMode ?? "List";
        StatusText = $"View changed to {CurrentViewMode}";
        UpdateViewModeInMenuItems();
        Task.Delay(2000).ContinueWith(_ => StatusText = "Ready");
    }

    private void OnSetSort(string sortMode)
    {
        System.Diagnostics.Debug.WriteLine($"OnSetSort command executed with: {sortMode}");
        CurrentSortMode = sortMode ?? "Name";
        StatusText = $"Sorted by {CurrentSortMode}";
        UpdateSortModeInMenuItems();
        Task.Delay(2000).ContinueWith(_ => StatusText = "Ready");
    }

    private void OnClear()
    {
        System.Diagnostics.Debug.WriteLine("OnClear command executed");
        ItemCount = 0;
        StatusText = "All items cleared";
        Task.Delay(2000).ContinueWith(_ => StatusText = "Ready");
    }

    private void OnAddItem()
    {
        System.Diagnostics.Debug.WriteLine("OnAddItem command executed");
        ItemCount++;
        StatusText = $"Added item #{ItemCount}";
        Task.Delay(2000).ContinueWith(_ => StatusText = "Ready");
    }

    private void OnFilterApplied(MenuNode menuNode)
    {
        System.Diagnostics.Debug.WriteLine($"OnFilterApplied called with: {menuNode?.Title}");
        if (menuNode != null)
        {
            StatusText = $"Filter applied: {menuNode.Title}";
            Task.Delay(2000).ContinueWith(_ => StatusText = "Ready");
        }
    }

    #endregion

    #region Menu Initialization

    private void InitializeMenuItems()
    {
        MenuItems.Clear();
        
        // Sort submenu
        MenuItems.Add(new MenuNode
        {
            Title = "Sort By",
            Children =
            {
                new MenuNode { Title = "Name", GroupKey = "sort", IsCheckable = true, IsChecked = true, Command = SetSortCommand, CommandParameter = "Name" },
                new MenuNode { Title = "Date", GroupKey = "sort", IsCheckable = true, Command = SetSortCommand, CommandParameter = "Date" },
                new MenuNode { Title = "Size", GroupKey = "sort", IsCheckable = true, Command = SetSortCommand, CommandParameter = "Size" },
                new MenuNode { Title = "Type", GroupKey = "sort", IsCheckable = true, Command = SetSortCommand, CommandParameter = "Type" }
            }
        });

        // Filter options
        MenuItems.Add(new MenuNode
        {
            Title = "Filters",
            Children =
            {
                new MenuNode { Title = "Show All", GroupKey = "filter", IsCheckable = true, IsChecked = true, CommandParameter = "all" },
                new MenuNode { Title = "Active Only", GroupKey = "filter", IsCheckable = true, CommandParameter = "active" },
                new MenuNode { Title = "Completed Only", GroupKey = "filter", IsCheckable = true, CommandParameter = "completed" }
            }
        });

        // Simple action
        MenuItems.Add(new MenuNode
        {
            Title = "Export Data",
            Command = new Command(() =>
            {
                StatusText = "Exporting data...";
                Task.Delay(2000).ContinueWith(_ => StatusText = "Export completed!");
                Task.Delay(4000).ContinueWith(_ => StatusText = "Ready");
            })
        });
    }

    private void UpdateViewModeInMenuItems()
    {
        // This would typically be handled automatically by the radio group logic
        // but we can also manually update for demonstration
    }

    private void UpdateSortModeInMenuItems()
    {
        // Find and update the sort group
        var sortMenu = MenuItems.FirstOrDefault(m => m.Title == "Sort By");
        if (sortMenu?.Children != null)
        {
            foreach (var child in sortMenu.Children)
            {
                child.IsChecked = child.CommandParameter?.ToString() == CurrentSortMode;
            }
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}

public enum Density
{
    Compact,
    Regular,
    Large,
}
