import Foundation

protocol PermissionsServiceProtocol: Sendable {
    func canManage(resource: PinnedResource) async -> Bool
}

@MainActor
@Observable
final class PermissionsService: PermissionsServiceProtocol {
    private let tokenCache: any TokenCacheProtocol
    private let session: URLSession
    private var cache: [String: Bool] = [:]

    init(tokenCache: any TokenCacheProtocol, session: URLSession = .shared) {
        self.tokenCache = tokenCache
        self.session = session
    }

    func canManage(resource: PinnedResource) async -> Bool {
        if let cached = cache[resource.id] { return cached }
        let result = await checkAccess(resource: resource)
        cache[resource.id] = result
        return result
    }

    private func checkAccess(resource: PinnedResource) async -> Bool {
        guard let token = try? await tokenCache.token(for: resource.subscriptionId) else { return false }
        let urlString = "https://management.azure.com\(resource.id)/providers/Microsoft.Authorization/checkAccess?api-version=2022-04-01"
        guard let url = URL(string: urlString) else { return false }
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        let body = ["actions": ["Microsoft.Web/sites/start/action", "Microsoft.Web/sites/stop/action"]]
        guard let bodyData = try? JSONSerialization.data(withJSONObject: body) else { return false }
        request.httpBody = bodyData
        guard let (data, _) = try? await session.data(for: request),
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let value = json["value"] as? [[String: Any]] else { return false }
        return !value.isEmpty
    }
}
