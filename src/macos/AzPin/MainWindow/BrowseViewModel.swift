import Foundation

@MainActor
@Observable
final class BrowseViewModel {
    private let azCLI: any AzCLIServiceProtocol
    private let arm: any ARMServiceProtocol

    var subscriptions: [AzureSubscription] = []
    var selectedSubscription: AzureSubscription?
    var isLoadingSubscriptions = false
    var resourceGroups: [AzureResourceGroup] = []
    var searchText: String = ""
    var isLoadingResourceGroups = false
    var selectedResourceGroupName: String?
    var resources: [AzureResource] = []
    var isLoadingResources = false
    var errorMessage: String?

    static func hiddenSubscriptionIds() -> Set<String> {
        let raw = UserDefaults.standard.string(forKey: "hiddenSubscriptionIds") ?? ""
        return Set(raw.split(separator: ",").map(String.init).filter { !$0.isEmpty })
    }

    var filteredResourceGroups: [AzureResourceGroup] {
        guard !searchText.isEmpty else { return resourceGroups }
        return resourceGroups.filter { $0.name.localizedCaseInsensitiveContains(searchText) }
    }

    init(azCLI: any AzCLIServiceProtocol, arm: any ARMServiceProtocol) {
        self.azCLI = azCLI
        self.arm = arm
    }

    func loadSubscriptions() async {
        isLoadingSubscriptions = true
        errorMessage = nil
        defer { isLoadingSubscriptions = false }
        do {
            let hiddenIds = Self.hiddenSubscriptionIds()
            subscriptions = try await azCLI.listSubscriptions()
                .filter { !hiddenIds.contains($0.id) }
                .sorted { lhs, rhs in
                    if lhs.isDefault != rhs.isDefault { return lhs.isDefault }
                    return lhs.tenantId < rhs.tenantId
                }
            if selectedSubscription == nil || !subscriptions.contains(where: { $0.id == selectedSubscription?.id }) {
                selectedSubscription = subscriptions.first
            }
            await loadResourceGroups()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadResourceGroups() async {
        guard let sub = selectedSubscription else { return }
        selectedResourceGroupName = nil
        resources = []
        errorMessage = nil
        isLoadingResourceGroups = true
        defer { isLoadingResourceGroups = false }
        do {
            let groups = try await arm.fetchResourceGroups(subscriptionId: sub.id, tenantId: sub.tenantId)
            guard selectedSubscription?.id == sub.id else { return }
            resourceGroups = groups.sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        } catch {
            guard selectedSubscription?.id == sub.id else { return }
            errorMessage = error.localizedDescription
        }
    }

    func loadResources(in rgName: String) async {
        guard let sub = selectedSubscription else { return }
        errorMessage = nil
        isLoadingResources = true
        defer { isLoadingResources = false }
        do {
            let res = try await arm.fetchResources(subscriptionId: sub.id, resourceGroup: rgName, tenantId: sub.tenantId)
            guard selectedResourceGroupName == rgName else { return }
            resources = res.sorted { $0.type.lowercased() < $1.type.lowercased() }
        } catch {
            guard selectedResourceGroupName == rgName else { return }
            errorMessage = error.localizedDescription
        }
    }
}
