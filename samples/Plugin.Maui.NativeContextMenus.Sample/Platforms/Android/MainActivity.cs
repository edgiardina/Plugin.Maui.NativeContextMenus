using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Plugin.Maui.NativeContextMenus;

namespace Plugin.Maui.Feature.Sample;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public override bool OnCreateOptionsMenu(IMenu menu)
    {
        base.OnCreateOptionsMenu(menu);
        
        System.Diagnostics.Debug.WriteLine("OnCreateOptionsMenu called");
        
        // Get current page and inject menu items for Android
        var currentPage = GetCurrentContentPage();
        if (currentPage != null)
        {
            System.Diagnostics.Debug.WriteLine($"Current page: {currentPage.GetType().Name}");
            
            if (AndroidMenuHelper.HasMenuToolbarItems(currentPage))
            {
                System.Diagnostics.Debug.WriteLine("Found NativeContextMenuToolbarItems, injecting...");
                var injected = AndroidMenuHelper.InjectToolbarMenuItems(menu, currentPage);
                System.Diagnostics.Debug.WriteLine($"Injection result: {injected}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No NativeContextMenuToolbarItems found");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Current page is null");
        }
        
        return true;
    }
    
    // Add this method to handle menu item clicks at the Activity level
    public override bool OnOptionsItemSelected(IMenuItem item)
    {
        System.Diagnostics.Debug.WriteLine($"OnOptionsItemSelected called for item: {item.TitleFormatted}");
        return base.OnOptionsItemSelected(item);
    }
    
    private ContentPage? GetCurrentContentPage()
    {
        if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage navPage)
        {
            return navPage.CurrentPage as ContentPage;
        }
        return Microsoft.Maui.Controls.Application.Current?.MainPage as ContentPage;
    }
}