using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using AzPin.Windows.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class ResourceItemViewModel : ObservableObject
{
    private readonly IPinService _pinService;

    public ArmResource Resource { get; }
    public string SubscriptionId { get; }
    public string ResourceGroupName { get; }

    public string ResourceId => Resource.Id;
    public string GlyphCode => ResourceTypeMapper.GlyphFor(Resource.Type);
    public string Name => Resource.Name;
    public string Type => Resource.Type;
    public Uri PortalUri => PortalUrl.ForResource(Resource.Id);

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsRgPinned { get; set; }

    public bool ShowPinButton => !IsRgPinned;

    public ResourceItemViewModel(ArmResource resource, string subscriptionId, string rgName, IPinService pinService)
    {
        Resource = resource;
        SubscriptionId = subscriptionId;
        ResourceGroupName = rgName;
        _pinService = pinService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsPinned = await _pinService.IsResourcePinnedAsync(Resource.Id, ct);
    }

    partial void OnIsRgPinnedChanged(bool value) => OnPropertyChanged(nameof(ShowPinButton));

    [RelayCommand]
    public async Task TogglePinAsync(CancellationToken ct = default)
    {
        if (IsPinned)
        {
            await _pinService.UnpinResourceAsync(Resource.Id, ct);
            IsPinned = false;
        }
        else
        {
            await _pinService.PinResourceAsync(Resource, SubscriptionId, ResourceGroupName, 0, ct);
            IsPinned = true;
        }
    }
}
