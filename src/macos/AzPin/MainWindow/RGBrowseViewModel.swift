import Foundation

@MainActor
@Observable
final class RGBrowseViewModel {
    private let arm: any ARMServiceProtocol

    var resources: [AzureResource] = []
    var isLoading = false
    var errorMessage: String?

    init(arm: any ARMServiceProtocol) {
        self.arm = arm
    }

    func loadResources(subscriptionId: String, rgName: String, tenantId: String) async {
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }
        do {
            let res = try await arm.fetchResources(
                subscriptionId: subscriptionId,
                resourceGroup: rgName,
                tenantId: tenantId
            )
            resources = res.sorted { $0.type.lowercased() < $1.type.lowercased() }
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
