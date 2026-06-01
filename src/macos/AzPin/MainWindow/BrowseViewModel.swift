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
    var isLoadingResourceGroups = false
    var selectedResourceGroupName: String?
    var resources: [AzureResource] = []
    var isLoadingResources = false
    var errorMessage: String?

    init(azCLI: any AzCLIServiceProtocol, arm: any ARMServiceProtocol) {
        self.azCLI = azCLI
        self.arm = arm
    }

    func loadSubscriptions() async {
        isLoadingSubscriptions = true
        errorMessage = nil
        defer { isLoadingSubscriptions = false }
        do {
            subscriptions = try await azCLI.listSubscriptions()
                .sorted { lhs, rhs in
                    if lhs.isDefault != rhs.isDefault { return lhs.isDefault }
                    return lhs.tenantId < rhs.tenantId
                }
            if selectedSubscription == nil {
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
            resourceGroups = groups
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
            resources = res
        } catch {
            guard selectedResourceGroupName == rgName else { return }
            errorMessage = error.localizedDescription
        }
    }
}
