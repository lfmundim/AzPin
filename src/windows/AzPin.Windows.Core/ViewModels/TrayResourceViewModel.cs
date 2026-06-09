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
    [NotifyPropertyChangedFor(nameof(ShowStartButton))]
    [NotifyPropertyChangedFor(nameof(ShowStopButton))]
    [NotifyPropertyChangedFor(nameof(ShowRestartButton))]
    [NotifyPropertyChangedFor(nameof(IsTransitional))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    public partial AppRunningState RunningState { get; set; } = AppRunningState.Unknown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStartButton))]
    [NotifyPropertyChangedFor(nameof(ShowStopButton))]
    [NotifyPropertyChangedFor(nameof(ShowRestartButton))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(RestartCommand))]
    public partial ResourcePermissions? Permissions { get; set; }

    [ObservableProperty]
    public partial string? ActionError { get; set; }

    public bool ShowStartButton =>
        IsRunnable && RunningState == AppRunningState.Stopped && (Permissions?.CanStart ?? false);
    public bool ShowStopButton =>
        IsRunnable && RunningState == AppRunningState.Running && (Permissions?.CanStop ?? false);
    public bool ShowRestartButton =>
        IsRunnable && RunningState == AppRunningState.Running && (Permissions?.CanRestart ?? false);

    public bool IsTransitional => RunningState is
        AppRunningState.Fetching or AppRunningState.Starting or
        AppRunningState.Stopping or AppRunningState.Restarting;

    public TrayResourceViewModel(ArmResource resource, string subscriptionId, IArmService arm)
    {
        Resource = resource;
        SubscriptionId = subscriptionId;
        _arm = arm;
    }

    [RelayCommand(CanExecute = nameof(ShowStartButton))]
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
            ShowActionError("Start failed. Check Azure Portal.");
        }
    }

    [RelayCommand(CanExecute = nameof(ShowStopButton))]
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
            ShowActionError("Stop failed. Check Azure Portal.");
        }
    }

    [RelayCommand(CanExecute = nameof(ShowRestartButton))]
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
            ShowActionError("Restart failed. Check Azure Portal.");
        }
    }

    private async void ShowActionError(string message)
    {
        ActionError = message;
        await Task.Delay(4000);
        ActionError = null;
    }
}
