using System.Collections.ObjectModel;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class RgBrowseViewModel : ObservableObject
{
    private readonly IArmService _arm;
    private readonly IPinService _pinService;

    private string _subscriptionId = string.Empty;
    private string _rgName = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<ResourceItemViewModel> Resources { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public RgBrowseViewModel(IArmService arm, IPinService pinService)
    {
        _arm = arm;
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
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var raw = await _arm.FetchResourcesAsync(_subscriptionId, string.Empty, _rgName, ct);
            var vms = raw.OrderBy(r => r.Type.ToLowerInvariant())
                         .Select(r => new ResourceItemViewModel(r, _subscriptionId, _rgName, _pinService))
                         .ToList();
            await Task.WhenAll(vms.Select(v => v.InitializeAsync(ct)));
            Resources = new ObservableCollection<ResourceItemViewModel>(vms);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
