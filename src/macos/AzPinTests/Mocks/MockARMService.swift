import Foundation
@testable import AzPin

final class MockARMService: ARMServiceProtocol, @unchecked Sendable {
    var resourceGroupsResult: Result<[AzureResourceGroup], Error> = .success([])
    var resourcesResult: Result<[AzureResource], Error> = .success([])
    var appStateResult: Result<AppRunningState, Error> = .success(.running)
    var startAppCalled = false
    var stopAppCalled = false
    var restartAppCalled = false
    var onFetchResourceGroups: (() -> Void)?
    var onFetchResources: (() -> Void)?

    func fetchResourceGroups(subscriptionId: String, tenantId: String) async throws -> [AzureResourceGroup] {
        onFetchResourceGroups?()
        return try resourceGroupsResult.get()
    }

    func fetchResources(subscriptionId: String, resourceGroup: String, tenantId: String) async throws -> [AzureResource] {
        onFetchResources?()
        return try resourcesResult.get()
    }

    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState {
        try appStateResult.get()
    }

    func startApp(resource: PinnedResource) async throws {
        startAppCalled = true
        if case .failure(let error) = appStateResult { throw error }
    }

    func stopApp(resource: PinnedResource) async throws {
        stopAppCalled = true
        if case .failure(let error) = appStateResult { throw error }
    }

    func restartApp(resource: PinnedResource) async throws {
        restartAppCalled = true
        if case .failure(let error) = appStateResult { throw error }
    }
}
