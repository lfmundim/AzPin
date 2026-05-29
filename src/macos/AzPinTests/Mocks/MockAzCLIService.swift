import Foundation
@testable import AzPin

final class MockAzCLIService: AzCLIServiceProtocol {
    var tokenResult: Result<AzureTokenResponse, Error> = .success(
        AzureTokenResponse(accessToken: "mock-token", expiresOn: .distantFuture)
    )
    var accountResult: Result<AzureAccount, Error> = .success(
        AzureAccount(id: "sub-1", name: "Test Sub", tenantId: "tenant-1", user: AzureAccountUser(name: "test@example.com"))
    )
    var subscriptionsResult: Result<[AzureSubscription], Error> = .success([])
    var installedResult = true

    func fetchToken(subscriptionId: String) async throws -> AzureTokenResponse {
        try tokenResult.get()
    }

    func currentAccount() async throws -> AzureAccount {
        try accountResult.get()
    }

    func listSubscriptions() async throws -> [AzureSubscription] {
        try subscriptionsResult.get()
    }

    func isInstalled() -> Bool {
        installedResult
    }
}
