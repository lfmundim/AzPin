import Foundation

protocol ARMServiceProtocol: Sendable {
    func fetchResourceGroups(subscriptionId: String) async throws -> [AzureResourceGroup]
    func fetchResources(subscriptionId: String, resourceGroup: String) async throws -> [AzureResource]
    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState
    func startApp(resource: PinnedResource) async throws
    func stopApp(resource: PinnedResource) async throws
    func restartApp(resource: PinnedResource) async throws
}

enum AppRunningState: Sendable {
    case running, stopped, unknown
}

@Observable
final class ARMService: ARMServiceProtocol {
    private let tokenCache: any TokenCacheProtocol
    private let session: URLSession

    init(tokenCache: any TokenCacheProtocol, session: URLSession = .shared) {
        self.tokenCache = tokenCache
        self.session = session
    }

    func fetchResourceGroups(subscriptionId: String) async throws -> [AzureResourceGroup] {
        let token = try await tokenCache.token(for: subscriptionId)
        let url = URL(string: "https://management.azure.com/subscriptions/\(subscriptionId)/resourcegroups?api-version=2021-04-01")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await session.data(for: request)
        return try JSONDecoder().decode(ResourceGroupListResponse.self, from: data).value
    }

    func fetchResources(subscriptionId: String, resourceGroup: String) async throws -> [AzureResource] {
        let token = try await tokenCache.token(for: subscriptionId)
        let url = URL(string: "https://management.azure.com/subscriptions/\(subscriptionId)/resourceGroups/\(resourceGroup)/resources?api-version=2021-04-01")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await session.data(for: request)
        return try JSONDecoder().decode(ResourceListResponse.self, from: data).value
    }

    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState {
        let token = try await tokenCache.token(for: resource.subscriptionId)
        let url = URL(string: "https://management.azure.com\(resource.id)?api-version=2023-01-01")!
        var request = URLRequest(url: url)
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        let (data, _) = try await session.data(for: request)
        let response = try JSONDecoder().decode(AppServiceResponse.self, from: data)
        return response.properties.state.lowercased() == "running" ? .running : .stopped
    }

    func startApp(resource: PinnedResource) async throws {
        try await performAction("start", on: resource)
    }

    func stopApp(resource: PinnedResource) async throws {
        try await performAction("stop", on: resource)
    }

    func restartApp(resource: PinnedResource) async throws {
        try await performAction("restart", on: resource)
    }

    private func performAction(_ action: String, on resource: PinnedResource) async throws {
        let token = try await tokenCache.token(for: resource.subscriptionId)
        let url = URL(string: "https://management.azure.com\(resource.id)/\(action)?api-version=2023-01-01")!
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("Bearer \(token)", forHTTPHeaderField: "Authorization")
        _ = try await session.data(for: request)
    }
}
