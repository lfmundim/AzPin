import XCTest
import SwiftData
@testable import AzPin

@MainActor
final class TokenCacheTests: XCTestCase {
    private var container: ModelContainer!
    private var context: ModelContext!
    private var mockCLI: MockAzCLIService!
    private var cache: TokenCache!

    override func setUpWithError() throws {
        container = try ModelContainer(
            for: CachedToken.self,
            configurations: ModelConfiguration(isStoredInMemoryOnly: true)
        )
        context = container.mainContext
        mockCLI = MockAzCLIService()
        cache = TokenCache(modelContext: context, azCLI: mockCLI)
    }

    override func tearDown() {
        cache = nil
        mockCLI = nil
        context = nil
        container = nil
    }

    func test_tokenIsReused_whenNotExpired() async throws {
        let stored = CachedToken(subscriptionId: "sub-1", tenantId: "", accessToken: "cached-token", expiresOn: .distantFuture)
        context.insert(stored)

        let token = try await cache.token(for: "sub-1", tenantId: "")

        XCTAssertEqual(token, "cached-token")
    }

    func test_tokenIsRefreshed_whenExpiredWithin5Minutes() async throws {
        let nearExpiry = Date.now.addingTimeInterval(3 * 60)
        let stored = CachedToken(subscriptionId: "sub-1", tenantId: "", accessToken: "old-token", expiresOn: nearExpiry)
        context.insert(stored)
        mockCLI.tokenResult = .success(AzureTokenResponse(accessToken: "fresh-token", expiresOn: .distantFuture))

        let token = try await cache.token(for: "sub-1", tenantId: "")

        XCTAssertEqual(token, "fresh-token")
    }

    func test_tokenIsRefreshed_whenMissing() async throws {
        mockCLI.tokenResult = .success(AzureTokenResponse(accessToken: "new-token", expiresOn: .distantFuture))

        let token = try await cache.token(for: "sub-1", tenantId: "")

        XCTAssertEqual(token, "new-token")
    }

    func test_refreshFailure_throws() async {
        mockCLI.tokenResult = .failure(URLError(.badServerResponse))

        do {
            _ = try await cache.token(for: "sub-1", tenantId: "")
            XCTFail("Expected throw")
        } catch {
            XCTAssertTrue(error is URLError)
        }
    }

    func test_invalidate_removesToken() async throws {
        let stored = CachedToken(subscriptionId: "sub-1", tenantId: "", accessToken: "old-token", expiresOn: .distantFuture)
        context.insert(stored)

        await cache.invalidate(subscriptionId: "sub-1")

        mockCLI.tokenResult = .success(AzureTokenResponse(accessToken: "refreshed", expiresOn: .distantFuture))
        let token = try await cache.token(for: "sub-1", tenantId: "")
        XCTAssertEqual(token, "refreshed")
    }
}
