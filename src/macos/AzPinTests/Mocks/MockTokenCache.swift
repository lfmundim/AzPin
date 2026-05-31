import Foundation
@testable import AzPin

final class MockTokenCache: TokenCacheProtocol, @unchecked Sendable {
    var tokenResult: Result<String, Error> = .success("mock-token")
    var invalidateCalled = false

    func token(for subscriptionId: String, tenantId: String) async throws -> String {
        try tokenResult.get()
    }

    func invalidate(subscriptionId: String) async {
        invalidateCalled = true
    }
}
