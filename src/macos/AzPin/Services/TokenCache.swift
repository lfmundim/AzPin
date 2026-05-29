import Foundation
import SwiftData

protocol TokenCacheProtocol: Sendable {
    func token(for subscriptionId: String) async throws -> String
    func invalidate(subscriptionId: String) async
}

@MainActor
@Observable
final class TokenCache: TokenCacheProtocol {
    private let modelContext: ModelContext
    private let azCLI: any AzCLIServiceProtocol
    private let expiryBuffer: TimeInterval = 5 * 60

    init(modelContext: ModelContext, azCLI: any AzCLIServiceProtocol) {
        self.modelContext = modelContext
        self.azCLI = azCLI
    }

    func token(for subscriptionId: String) async throws -> String {
        let descriptor = FetchDescriptor<CachedToken>(
            predicate: #Predicate { $0.subscriptionId == subscriptionId }
        )
        if let cached = try modelContext.fetch(descriptor).first,
           cached.expiresOn > Date.now.addingTimeInterval(expiryBuffer) {
            return cached.accessToken
        }
        return try await refresh(subscriptionId: subscriptionId)
    }

    func invalidate(subscriptionId: String) async {
        let descriptor = FetchDescriptor<CachedToken>(
            predicate: #Predicate { $0.subscriptionId == subscriptionId }
        )
        guard let cached = try? modelContext.fetch(descriptor).first else { return }
        modelContext.delete(cached)
    }

    private func refresh(subscriptionId: String) async throws -> String {
        let response = try await azCLI.fetchToken(subscriptionId: subscriptionId)
        let descriptor = FetchDescriptor<CachedToken>(
            predicate: #Predicate { $0.subscriptionId == subscriptionId }
        )
        let existing = try? modelContext.fetch(descriptor).first
        let cached = existing ?? CachedToken(subscriptionId: subscriptionId, tenantId: "", accessToken: "", expiresOn: .now)
        cached.accessToken = response.accessToken
        cached.expiresOn = response.expiresOn
        if existing == nil { modelContext.insert(cached) }
        return cached.accessToken
    }
}
