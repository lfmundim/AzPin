import Foundation

@MainActor
@Observable
final class BrowseViewModel {
    private let azCLI: any AzCLIServiceProtocol
    private let arm: any ARMServiceProtocol

    var subscriptions: [AzureSubscription] = []
    var selectedSubscriptionId: String?
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
            if selectedSubscriptionId == nil {
                selectedSubscriptionId = subscriptions.first?.id
            }
            await loadResourceGroups()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadResourceGroups() async {
        guard let subId = selectedSubscriptionId else { return }
        selectedResourceGroupName = nil
        resources = []
        isLoadingResourceGroups = true
        defer { isLoadingResourceGroups = false }
        do {
            resourceGroups = try await arm.fetchResourceGroups(subscriptionId: subId)
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func loadResources(in rgName: String) async {
        guard let subId = selectedSubscriptionId else { return }
        isLoadingResources = true
        defer { isLoadingResources = false }
        do {
            resources = try await arm.fetchResources(subscriptionId: subId, resourceGroup: rgName)
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
