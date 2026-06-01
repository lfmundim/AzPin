import Foundation

protocol PermissionsServiceProtocol: Sendable {
    func canManage(resource: PinnedResource) async -> Bool
}

@Observable
final class PermissionsService: PermissionsServiceProtocol {
    private let tokenCache: any TokenCacheProtocol
    private let session: URLSession
    private let cache = Cache()

    init(tokenCache: any TokenCacheProtocol, session: URLSession = .shared) {
        self.tokenCache = tokenCache
        self.session = session
    }

    func canManage(resource: PinnedResource) async -> Bool {
        if let cached = await cache.get(resource.id) { return cached }
        let result = await checkAccess(resource: resource)
        await cache.set(resource.id, value: result)
        return result
    }

    // Uses GET .../providers/Microsoft.Authorization/permissions which is accessible
    // to Contributors. The previous checkAccess POST required Owner-level
    // Microsoft.Authorization/*/read, causing Contributors to always get false.
    private func checkAccess(resource: PinnedResource) async -> Bool {
        guard let token = try? await tokenCache.token(for: resource.subscriptionId) else { return false }
        let urlString = "https://management.azure.com\(resource.id)/providers/Microsoft.Authorization/permissions?api-version=2022-04-01"
        guard let url = URL(string: urlString) else { return false }
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        guard let (data, _) = try? await session.data(for: request),
              let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let value = json["value"] as? [[String: Any]] else { return false }
        // Each entry has "actions" (allowed) and "notActions" (denied). Check that
        // at least one entry grants a wildcard or explicit start/stop permission.
        let startAction = "microsoft.web/sites/start/action"
        let stopAction = "microsoft.web/sites/stop/action"
        for entry in value {
            guard let actions = entry["actions"] as? [String] else { continue }
            let notActions = (entry["notActions"] as? [String]) ?? []
            let normalized = actions.map { $0.lowercased() }
            let deniedNormalized = notActions.map { $0.lowercased() }
            let grants = normalized.contains("*") ||
                         normalized.contains(startAction) ||
                         normalized.contains(stopAction)
            let denied = deniedNormalized.contains(startAction) ||
                         deniedNormalized.contains(stopAction)
            if grants && !denied { return true }
        }
        return false
    }

    private actor Cache {
        private var storage: [String: Bool] = [:]

        func get(_ key: String) -> Bool? {
            storage[key]
        }

        func set(_ key: String, value: Bool) {
            storage[key] = value
        }
    }
}
