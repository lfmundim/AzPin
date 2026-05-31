import Foundation

@MainActor
@Observable
final class BrowseViewModel {
    private let azCLI: any AzCLIServiceProtocol
    private let arm: any ARMServiceProtocol

    var subscriptions: [AzureSubscription] = []
    var selectedSubscriptionId: String?
    var isLoadingSubscriptions = false
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
        } catch {
            errorMessage = error.localizedDescription
        }
    }
}
