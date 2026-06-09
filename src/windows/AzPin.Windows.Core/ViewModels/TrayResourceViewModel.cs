using AzPin.Windows.Models;
using AzPin.Windows.Models.Arm;
using AzPin.Windows.Services;
using AzPin.Windows.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class TrayResourceViewModel : ObservableObject
{
    private readonly IArmService _arm;

    public ArmResource Resource { get; }
    public string SubscriptionId { get; }
    public string Name => Resource.Name;
    public string GlyphCode => ResourceTypeMapper.GlyphFor(Resource.Type);
    public Uri PortalUri => PortalUrl.ForResource(Resource.Id);
    public bool IsRunnable => ResourceTypeMapper.IsRunnable(Resource.Type);

    [ObservableProperty]
    public partial AppRunningState RunningState { get; set; } = AppRunningState.Unknown;

    public TrayResourceViewModel(ArmResource resource, string subscriptionId, IArmService arm)
    {
        Resource = resource;
        SubscriptionId = subscriptionId;
        _arm = arm;
    }

    [RelayCommand]
    public async Task StartAsync(CancellationToken ct = default)
    {
        var prev = RunningState;
        RunningState = AppRunningState.Starting;
        try
        {
            await _arm.StartResourceAsync(SubscriptionId, string.Empty, Resource, ct);
            RunningState = AppRunningState.Running;
        }
        catch
        {
            RunningState = prev;
        }
    }

    [RelayCommand]
    public async Task StopAsync(CancellationToken ct = default)
    {
        var prev = RunningState;
        RunningState = AppRunningState.Stopping;
        try
        {
            await _arm.StopResourceAsync(SubscriptionId, string.Empty, Resource, ct);
            RunningState = AppRunningState.Stopped;
        }
        catch
        {
            RunningState = prev;
        }
    }

    [RelayCommand]
    public async Task RestartAsync(CancellationToken ct = default)
    {
        var prev = RunningState;
        RunningState = AppRunningState.Restarting;
        try
        {
            await _arm.RestartResourceAsync(SubscriptionId, string.Empty, Resource, ct);
            RunningState = AppRunningState.Running;
        }
        catch
        {
            RunningState = prev;
        }
    }
}
