using AzPin.Windows.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace AzPin.Windows.TrayIcon;

public partial class TrayMenuViewModel(AuthViewModel auth) : ObservableObject
{
    private readonly AuthViewModel _auth = auth;

    public AuthViewModel Auth => _auth;

    [RelayCommand]
    public async Task OnMenuOpenedAsync()
    {
        await _auth.RefreshAsync();
    }

    [RelayCommand]
    public static void Quit()
    {
        Application.Current.Exit();
    }
}
