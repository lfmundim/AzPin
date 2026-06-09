using System.Collections.ObjectModel;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IPinService _pinService;

    [ObservableProperty]
    public partial ObservableCollection<SidebarRgItemViewModel> PinnedRgs { get; set; } = [];

    [ObservableProperty]
    public partial SidebarRgItemViewModel? SelectedRg { get; set; }

    public MainWindowViewModel(IPinService pinService)
    {
        _pinService = pinService;
        pinService.PinsChanged += () => _ = LoadPinnedRgsAsync();
    }

    [RelayCommand]
    public async Task LoadPinnedRgsAsync(CancellationToken ct = default)
    {
        var rgs = await _pinService.GetPinnedResourceGroupsAsync(ct);

        var duplicateNames = rgs
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        PinnedRgs = new ObservableCollection<SidebarRgItemViewModel>(
            rgs.OrderBy(r => r.DisplayOrder)
               .Select(r => new SidebarRgItemViewModel(r, duplicateNames.Contains(r.Name))));
    }

    [RelayCommand]
    public async Task UnpinRgAsync(SidebarRgItemViewModel item, CancellationToken ct = default)
    {
        await _pinService.UnpinResourceGroupAsync(item.SubscriptionId, item.Name, ct);
        PinnedRgs.Remove(item);
        if (SelectedRg == item)
            SelectedRg = null;
    }

    public async Task PersistOrderAsync(CancellationToken ct = default)
    {
        var updates = PinnedRgs
            .Select((vm, idx) => (vm.LocalId, DisplayOrder: idx))
            .ToList();
        await _pinService.UpdateDisplayOrderAsync(updates, ct);
    }
}
