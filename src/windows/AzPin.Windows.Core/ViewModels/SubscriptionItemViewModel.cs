using AzPin.Windows.Models;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AzPin.Windows.ViewModels;

public partial class SubscriptionItemViewModel : ObservableObject
{
    private readonly ISubscriptionSettingsService _service;
    private bool _initializing;

    public AzSubscription Subscription { get; }
    public string Name => Subscription.Name;
    public string Id => Subscription.Id;
    public bool IsDefault => Subscription.IsDefault;

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    public SubscriptionItemViewModel(AzSubscription subscription, bool isVisible, ISubscriptionSettingsService service)
    {
        Subscription = subscription;
        _service = service;
        _initializing = true;
        IsVisible = isVisible;
        _initializing = false;
    }

    partial void OnIsVisibleChanged(bool value)
    {
        if (_initializing) return;
        _ = _service.SetVisibilityAsync(Subscription.Id, value);
    }
}
