using AzPin.Windows.Services;
using AzPin.Windows.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    private readonly IAzCliService _azCli;
    private PeriodicTimer? _timer;

    public enum StepState { Pending, Checking, Pass, Fail }

    [ObservableProperty] public partial StepState CliState { get; set; } = StepState.Pending;
    [ObservableProperty] public partial StepState AuthState { get; set; } = StepState.Pending;
    [ObservableProperty] public partial StepState SubscriptionState { get; set; } = StepState.Pending;
    [ObservableProperty] public partial bool CanGetStarted { get; set; }
    [ObservableProperty] public partial string? CliHelpText { get; set; }
    [ObservableProperty] public partial string? AuthHelpText { get; set; }
    [ObservableProperty] public partial string? SubscriptionHelpText { get; set; }

    public OnboardingViewModel(IAzCliService azCli)
    {
        _azCli = azCli;
    }

    public void StartPolling()
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        _ = PollLoop();
    }

    public void StopPolling()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async Task PollLoop()
    {
        while (_timer is not null && await _timer.WaitForNextTickAsync())
            await CheckAllStepsAsync();
    }

    public async Task CheckAllStepsAsync(CancellationToken ct = default)
    {
        CliState = StepState.Checking;
        CliState = _azCli.IsCliInstalled ? StepState.Pass : StepState.Fail;
        CliHelpText = CliState == StepState.Fail
            ? "Install the Azure CLI: https://aka.ms/installazurecliwindows"
            : null;

        if (CliState != StepState.Pass)
        {
            AuthState = StepState.Pending;
            SubscriptionState = StepState.Pending;
            AuthHelpText = null;
            SubscriptionHelpText = null;
            CanGetStarted = false;
            return;
        }

        AuthState = StepState.Checking;
        var account = await _azCli.GetCurrentAccountAsync(ct);
        AuthState = account is not null ? StepState.Pass : StepState.Fail;
        AuthHelpText = AuthState == StepState.Fail ? "Run 'az login' in a terminal." : null;

        if (AuthState != StepState.Pass)
        {
            SubscriptionState = StepState.Pending;
            SubscriptionHelpText = null;
            CanGetStarted = false;
            return;
        }

        SubscriptionState = StepState.Checking;
        var subs = await _azCli.ListSubscriptionsAsync(ct);
        SubscriptionState = subs.Count > 0 ? StepState.Pass : StepState.Fail;
        SubscriptionHelpText = SubscriptionState == StepState.Fail
            ? "Ensure your account has access to at least one Azure subscription."
            : null;

        CanGetStarted = SubscriptionState == StepState.Pass;
        if (CanGetStarted) StopPolling();
    }

    [RelayCommand]
    public void CompleteOnboarding()
    {
        StopPolling();
        AppSettings.SetOnboardingCompleted(true);
    }
}
