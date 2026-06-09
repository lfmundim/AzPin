using AzPin.Windows.Models.Entities;
using AzPin.Windows.Utilities;

namespace AzPin.Windows.ViewModels;

public class SidebarRgItemViewModel
{
    private readonly PinnedResourceGroup _rg;
    private readonly bool _showSubscription;

    public int LocalId => _rg.LocalId;
    public string Name => _rg.Name;
    public string SubscriptionId => _rg.SubscriptionId;
    public int DisplayOrder => _rg.DisplayOrder;
    public Uri PortalUri => PortalUrl.ForResourceGroup(_rg.SubscriptionId, _rg.Name);

    public string DisplayName => _showSubscription && !string.IsNullOrEmpty(_rg.SubscriptionId)
        ? $"{_rg.Name} · {_rg.SubscriptionId[..Math.Min(8, _rg.SubscriptionId.Length)]}"
        : _rg.Name;

    public SidebarRgItemViewModel(PinnedResourceGroup rg, bool showSubscription = false)
    {
        _rg = rg;
        _showSubscription = showSubscription;
    }
}
