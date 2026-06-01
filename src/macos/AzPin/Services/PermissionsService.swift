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
        // at least one entry grants start/stop via exact match, trailing wildcard
        // (e.g. Microsoft.Web/sites/*), or full wildcard — without a matching deny.
        let startAction = "microsoft.web/sites/start/action"
        let stopAction  = "microsoft.web/sites/stop/action"
        for entry in value {
            guard let actions = entry["actions"] as? [String] else { continue }
            let notActions = (entry["notActions"] as? [String]) ?? []
            let grants = actions.contains { Self.armPattern($0, matches: startAction) ||
                                            Self.armPattern($0, matches: stopAction) }
            let denied = notActions.contains { Self.armPattern($0, matches: startAction) ||
                                               Self.armPattern($0, matches: stopAction) }
            if grants && !denied { return true }
        }
        return false
    }

    // Evaluates an ARM RBAC action pattern against a concrete action string.
    // Handles: exact match, full wildcard "*", and trailing-segment wildcards
    // such as "Microsoft.Web/sites/*" or "Microsoft.Web/*".
    private static func armPattern(_ pattern: String, matches action: String) -> Bool {
        let p = pattern.lowercased()
        let a = action.lowercased()
        if p == "*" || p == a { return true }
        if p.hasSuffix("/*") {
            let prefix = String(p.dropLast(2))
            return a.hasPrefix(prefix + "/") || a == prefix
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
