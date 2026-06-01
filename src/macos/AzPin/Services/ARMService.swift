import Foundation

protocol ARMServiceProtocol: Sendable {
    func fetchResourceGroups(subscriptionId: String, tenantId: String) async throws -> [AzureResourceGroup]
    func fetchResources(subscriptionId: String, resourceGroup: String, tenantId: String) async throws -> [AzureResource]
    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState
    func startApp(resource: PinnedResource) async throws
    func stopApp(resource: PinnedResource) async throws
    func restartApp(resource: PinnedResource) async throws
}

extension ARMServiceProtocol {
    func fetchResourceGroups(subscriptionId: String) async throws -> [AzureResourceGroup] {
        try await fetchResourceGroups(subscriptionId: subscriptionId, tenantId: "")
    }

    func fetchResources(subscriptionId: String, resourceGroup: String) async throws -> [AzureResource] {
        try await fetchResources(subscriptionId: subscriptionId, resourceGroup: resourceGroup, tenantId: "")
    }
}

enum AppRunningState: Sendable {
    case running, stopped, unknown, starting, stopping, restarting
}

@Observable
final class ARMService: ARMServiceProtocol {
    private let tokenCache: any TokenCacheProtocol
    private let session: URLSession

    init(tokenCache: any TokenCacheProtocol, session: URLSession = .shared) {
        self.tokenCache = tokenCache
        self.session = session
    }

    func fetchResourceGroups(subscriptionId: String, tenantId: String) async throws -> [AzureResourceGroup] {
        let token = try await tokenCache.token(for: subscriptionId, tenantId: tenantId)
        let url = URL(string: "https://management.azure.com/subscriptions/\(subscriptionId)/resourcegroups?api-version=2021-04-01")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await session.data(for: request)
        return try JSONDecoder().decode(ResourceGroupListResponse.self, from: data).value
    }

    func fetchResources(subscriptionId: String, resourceGroup: String, tenantId: String) async throws -> [AzureResource] {
        let token = try await tokenCache.token(for: subscriptionId, tenantId: tenantId)
        let url = URL(string: "https://management.azure.com/subscriptions/\(subscriptionId)/resourceGroups/\(resourceGroup)/resources?api-version=2021-04-01")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await session.data(for: request)
        return try JSONDecoder().decode(ResourceListResponse.self, from: data).value
    }

    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState {
        let token = try await tokenCache.token(for: resource.subscriptionId)
        let apiVer = Self.apiVersion(for: resource.type)
        let url = URL(string: "https://management.azure.com\(resource.id)?api-version=\(apiVer)")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await session.data(for: request)
        return try Self.parseAppState(from: data, resourceType: resource.type)
    }

    func startApp(resource: PinnedResource) async throws {
        try await performAction("start", on: resource)
    }

    func stopApp(resource: PinnedResource) async throws {
        try await performAction("stop", on: resource)
    }

    func restartApp(resource: PinnedResource) async throws {
        // Container Apps have no restart endpoint; simulate with stop → start.
        if resource.type.lowercased() == "microsoft.app/containerapps" {
            try await performAction("stop", on: resource)
            try await performAction("start", on: resource)
        } else {
            try await performAction("restart", on: resource)
        }
    }

    private func performAction(_ action: String, on resource: PinnedResource) async throws {
        let token = try await tokenCache.token(for: resource.subscriptionId)
        let apiVer = Self.apiVersion(for: resource.type)
        let resolvedAction = Self.mapAction(action, for: resource.type)
        let url = URL(string: "https://management.azure.com\(resource.id)/\(resolvedAction)?api-version=\(apiVer)")!
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        _ = try await session.data(for: request)
    }

    static func apiVersion(for resourceType: String) -> String {
        switch resourceType.lowercased() {
        case "microsoft.app/containerapps": return "2023-05-01"
        case "microsoft.logic/workflows":   return "2019-05-01"
        default:                            return "2023-01-01"
        }
    }

    static func mapAction(_ action: String, for resourceType: String) -> String {
        guard resourceType.lowercased() == "microsoft.logic/workflows" else { return action }
        switch action {
        case "start": return "enable"
        case "stop":  return "disable"
        default:      return action
        }
    }

    static func parseAppState(from data: Data, resourceType: String) throws -> AppRunningState {
        switch resourceType.lowercased() {
        case "microsoft.app/containerapps":
            let response = try JSONDecoder().decode(ContainerAppResponse.self, from: data)
            return response.properties.runningStatus.lowercased() == "running" ? .running : .stopped
        case "microsoft.logic/workflows":
            let response = try JSONDecoder().decode(AppServiceResponse.self, from: data)
            return response.properties.state.lowercased() == "enabled" ? .running : .stopped
        default:
            let response = try JSONDecoder().decode(AppServiceResponse.self, from: data)
            return response.properties.state.lowercased() == "running" ? .running : .stopped
        }
    }
}
