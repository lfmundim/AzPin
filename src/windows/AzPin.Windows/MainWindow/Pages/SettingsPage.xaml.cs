using AzPin.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace AzPin.Windows.MainWindow.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        TabBar.SelectedItem = AccountTab;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (!ViewModel.Auth.IsRefreshing)
            await ViewModel.Auth.RefreshAsync();
        await ViewModel.LoadAsync();
    }

    private void OnTabSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var tag = (sender.SelectedItem as SelectorBarItem)?.Tag as string;
        AccountPanel.Visibility       = tag == "Account"       ? Visibility.Visible : Visibility.Collapsed;
        SubscriptionsPanel.Visibility = tag == "Subscriptions" ? Visibility.Visible : Visibility.Collapsed;
        PreferencesPanel.Visibility   = tag == "Preferences"   ? Visibility.Visible : Visibility.Collapsed;
    }
}
