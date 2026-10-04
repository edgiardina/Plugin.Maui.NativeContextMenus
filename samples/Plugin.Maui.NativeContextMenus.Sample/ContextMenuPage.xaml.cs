using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Plugin.Maui.Feature.Sample;

public partial class ContextMenuPage : ContentPage
{
    public ContextMenuPage()
    {
        InitializeComponent();
        BindingContext = new ContextMenuViewModel();
    }
}

public class ContextMenuViewModel : INotifyPropertyChanged
{
    string lastAction = "None";
    bool isFavorite;

    public ContextMenuViewModel()
    {
        RunCommand = new Command<string>(name => LastAction = name);
        RenameCommand = new Command<string>(machine =>
        {
            Machines[Machines.IndexOf(machine)] = $"{machine} (renamed)";
            LastAction = $"Rename {machine}";
        });
        RemoveCommand = new Command<string>(machine =>
        {
            Machines.Remove(machine);
            LastAction = $"Remove {machine}";
        });
    }

    public ObservableCollection<string> Machines { get; } = new()
    {
        "Grand Prix", "Medieval Madness", "Attack from Mars", "Twilight Zone", "Godzilla",
    };

    public ICommand RunCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand RemoveCommand { get; }

    public string LastAction
    {
        get => lastAction;
        set => Set(ref lastAction, value);
    }

    public bool IsFavorite
    {
        get => isFavorite;
        set => Set(ref isFavorite, value);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
