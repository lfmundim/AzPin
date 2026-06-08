using System.Collections.ObjectModel;
using AzPin.Windows.Services;
using AzPin.Windows.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.TrayIcon;

public partial class TrayMenuViewModel : ObservableObject
{
    private readonly AuthViewModel _auth;
    private readonly IPinService _pinService;
    private readonly Action _quit;
    private readonly Action _openMainWindow;

    public AuthViewModel Auth => _auth;

    [ObservableProperty]
    public partial ObservableCollection<PinnedResourceItemViewModel> PinnedResources { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoadingPinnedResources { get; set; }

    public TrayMenuViewModel(AuthViewModel auth, IPinService pinService, Action quit, Action openMainWindow)
    {
        _auth = auth;
        _pinService = pinService;
        _quit = quit;
        _openMainWindow = openMainWindow;
    }

    [RelayCommand]
    public async Task OnMenuOpenedAsync()
    {
        await _auth.RefreshAsync();
        await LoadPinnedResourcesAsync();
    }

    private async Task LoadPinnedResourcesAsync()
    {
        IsLoadingPinnedResources = true;
        try
        {
            var pinned = await _pinService.GetPinnedResourcesAsync();
            PinnedResources = new ObservableCollection<PinnedResourceItemViewModel>(
                pinned.OrderBy(p => p.DisplayOrder)
                      .Select(p => new PinnedResourceItemViewModel(p)));
        }
        finally
        {
            IsLoadingPinnedResources = false;
        }
    }

    [RelayCommand]
    private void OpenMainWindow() => _openMainWindow();

    [RelayCommand]
    private void Quit() => _quit();
}
