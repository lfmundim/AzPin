using System.Collections.ObjectModel;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class PinnedResourcesViewModel : ObservableObject
{
    private readonly IPinService _pinService;
    private string _subscriptionId = string.Empty;
    private string _rgName = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<PinnedResourceItemViewModel> Resources { get; set; } = [];

    public PinnedResourcesViewModel(IPinService pinService)
    {
        _pinService = pinService;
    }

    public void Initialize(string subscriptionId, string rgName)
    {
        _subscriptionId = subscriptionId;
        _rgName = rgName;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var all = await _pinService.GetPinnedResourcesAsync(ct);
        var filtered = all
            .Where(r => r.SubscriptionId == _subscriptionId
                     && r.ResourceGroup.Equals(_rgName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.DisplayOrder);
        Resources = new ObservableCollection<PinnedResourceItemViewModel>(
            filtered.Select(r => new PinnedResourceItemViewModel(r)));
    }

    [RelayCommand]
    public async Task UnpinAsync(PinnedResourceItemViewModel item, CancellationToken ct = default)
    {
        await _pinService.UnpinResourceAsync(item.ResourceId, ct);
        Resources.Remove(item);
    }

    public async Task PersistOrderAsync(CancellationToken ct = default)
    {
        // PinnedResource uses LocalId for ordering — use PinService.UpdateDisplayOrderAsync
        // Note: resources are PinnedResource entities; we need their LocalId
        // For now, no-op since PinnedResourceItemViewModel doesn't expose LocalId
        await Task.CompletedTask;
    }
}
