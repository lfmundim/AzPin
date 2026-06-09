using System.Collections.ObjectModel;
using AzPin.Windows.Models;
using AzPin.Windows.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AzPin.Windows.ViewModels;

public partial class BrowseViewModel : ObservableObject
{
    private readonly IAzCliService _azCli;
    private readonly IArmService _arm;
    private readonly IPinService _pinService;

    [ObservableProperty]
    public partial ObservableCollection<AzSubscription> Subscriptions { get; set; } = [];

    [ObservableProperty]
    public partial AzSubscription? SelectedSubscription { get; set; }

    [ObservableProperty]
    public partial bool IsLoadingSubscriptions { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ResourceGroupItemViewModel> ResourceGroups { get; set; } = [];

    [ObservableProperty]
    public partial bool IsLoadingResourceGroups { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public IEnumerable<ResourceGroupItemViewModel> FilteredResourceGroups =>
        string.IsNullOrEmpty(SearchText)
            ? ResourceGroups
            : ResourceGroups.Where(rg =>
                rg.ResourceGroup.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

    public bool HasError => ErrorMessage is not null;

    public bool ShowNoSubscriptions =>
        !IsLoadingSubscriptions && ErrorMessage is null && Subscriptions.Count == 0;

    public bool ShowSelectPrompt =>
        !IsLoadingSubscriptions && SelectedSubscription is null && Subscriptions.Count > 0;

    public bool ShowRgArea =>
        !IsLoadingSubscriptions && SelectedSubscription is not null && ErrorMessage is null;

    public bool ShowNoRgs =>
        ShowRgArea && !IsLoadingResourceGroups && ResourceGroups.Count == 0;

    public bool ShowNoSearchResults =>
        ShowRgArea && !IsLoadingResourceGroups && ResourceGroups.Count > 0 && !FilteredResourceGroups.Any();

    public bool ShowRgList =>
        ShowRgArea && !IsLoadingResourceGroups && FilteredResourceGroups.Any();

    public BrowseViewModel(IAzCliService azCli, IArmService arm, IPinService pinService)
    {
        _azCli = azCli;
        _arm = arm;
        _pinService = pinService;
    }

    private void RaiseVisibilityProps()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(ShowNoSubscriptions));
        OnPropertyChanged(nameof(ShowSelectPrompt));
        OnPropertyChanged(nameof(ShowRgArea));
        OnPropertyChanged(nameof(ShowNoRgs));
        OnPropertyChanged(nameof(ShowNoSearchResults));
        OnPropertyChanged(nameof(ShowRgList));
        OnPropertyChanged(nameof(FilteredResourceGroups));
    }

    partial void OnSearchTextChanged(string value) => RaiseVisibilityProps();
    partial void OnIsLoadingSubscriptionsChanged(bool value) => RaiseVisibilityProps();
    partial void OnIsLoadingResourceGroupsChanged(bool value) => RaiseVisibilityProps();
    partial void OnErrorMessageChanged(string? value) => RaiseVisibilityProps();
    partial void OnSubscriptionsChanged(ObservableCollection<AzSubscription> value) => RaiseVisibilityProps();
    partial void OnResourceGroupsChanged(ObservableCollection<ResourceGroupItemViewModel> value) => RaiseVisibilityProps();

    partial void OnSelectedSubscriptionChanged(AzSubscription? value)
    {
        RaiseVisibilityProps();
        if (value is not null)
            _ = LoadResourceGroupsAsync();
    }

    [RelayCommand]
    public async Task LoadSubscriptionsAsync(CancellationToken ct = default)
    {
        IsLoadingSubscriptions = true;
        ErrorMessage = null;
        try
        {
            var subs = await _azCli.ListSubscriptionsAsync(ct);
            Subscriptions = new ObservableCollection<AzSubscription>(subs);
            SelectedSubscription = subs.FirstOrDefault(s => s.IsDefault) ?? subs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoadingSubscriptions = false;
        }
    }

    [RelayCommand]
    public async Task LoadResourceGroupsAsync(CancellationToken ct = default)
    {
        if (SelectedSubscription is null) return;
        IsLoadingResourceGroups = true;
        ErrorMessage = null;
        ResourceGroups.Clear();
        SearchText = string.Empty;

        var sub = SelectedSubscription;
        try
        {
            var rgs = await _arm.FetchResourceGroupsAsync(sub.Id, sub.TenantId, ct);
            // Guard: user may have changed selection while loading
            if (SelectedSubscription?.Id != sub.Id) return;

            foreach (var rg in rgs.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
                ResourceGroups.Add(new ResourceGroupItemViewModel(rg, sub.Id, sub.TenantId, _arm, _pinService));
        }
        catch (Exception ex)
        {
            if (SelectedSubscription?.Id == sub.Id)
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            IsLoadingResourceGroups = false;
        }
    }
}
