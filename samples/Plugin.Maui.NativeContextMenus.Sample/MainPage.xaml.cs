using Plugin.Maui.NativeContextMenus;

namespace Plugin.Maui.NativeContextMenus.Sample;

public partial class MainPage : ContentPage
{
    public MainPageViewModel ViewModel { get; }

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        BindingContext = viewModel;
    }
}
