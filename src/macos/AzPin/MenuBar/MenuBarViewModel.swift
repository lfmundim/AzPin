import Foundation

@MainActor
@Observable
final class MenuBarViewModel {
    private let arm: any ARMServiceProtocol
    private let permissionsService: any PermissionsServiceProtocol

    var resourcesByRG: [String: [AzureResource]] = [:]
    var loadErrors: [String: String] = [:]
    var expandedRGId: String?
    var isLoading = false
    var appStates: [String: AppRunningState] = [:]
    var permissions: [String: Bool] = [:]

    init(arm: any ARMServiceProtocol, permissionsService: any PermissionsServiceProtocol) {
        self.arm = arm
        self.permissionsService = permissionsService
    }

    func toggle(rgId: String) {
        expandedRGId = expandedRGId == rgId ? nil : rgId
    }

    func loadResources(for pinnedGroups: [PinnedResourceGroup]) async {
        isLoading = true
        defer { isLoading = false }
        // Capture primitive values before entering task group to avoid sending @Model objects across isolation boundaries
        let rgTuples: [(id: String, subscriptionId: String, name: String)] = pinnedGroups.map { ($0.id, $0.subscriptionId, $0.name) }
        await withTaskGroup(of: (String, [AzureResource]?, String?).self) { group in
            for rg in rgTuples {
                group.addTask {
                    do {
                        let resources = try await self.arm.fetchResources(
                            subscriptionId: rg.subscriptionId,
                            resourceGroup: rg.name
                        )
                        return (rg.id, resources, nil)
                    } catch {
                        return (rg.id, nil, error.localizedDescription)
                    }
                }
            }
            for await (rgId, resources, error) in group {
                if let resources {
                    resourcesByRG[rgId] = resources
                    loadErrors[rgId] = nil
                }
                if let error { loadErrors[rgId] = error }
                if let resources, let rg = rgTuples.first(where: { $0.id == rgId }) {
                    Task {
                        async let states: Void = fetchStates(for: resources, subscriptionId: rg.subscriptionId, resourceGroup: rg.name)
                        async let perms: Void = checkPermissions(for: resources, subscriptionId: rg.subscriptionId, resourceGroup: rg.name)
                        _ = await (states, perms)
                    }
                }
            }
        }
    }

    func fetchStates(for resources: [AzureResource], subscriptionId: String, resourceGroup: String) async {
        let runnable = resources.filter { ResourceTypeMapper.isRunnable($0.type) }
        await withTaskGroup(of: (String, AppRunningState).self) { group in
            for resource in runnable {
                group.addTask {
                    let pinned = PinnedResource(
                        id: resource.id, name: resource.name, type: resource.type,
                        resourceGroup: resourceGroup, subscriptionId: subscriptionId,
                        location: resource.location, displayOrder: 0
                    )
                    let state = (try? await self.arm.fetchAppState(resource: pinned)) ?? .unknown
                    return (resource.id, state)
                }
            }
            for await (id, state) in group {
                appStates[id] = state
            }
        }
    }

    func checkPermissions(for resources: [AzureResource], subscriptionId: String, resourceGroup: String) async {
        let runnable = resources.filter { ResourceTypeMapper.isRunnable($0.type) }
        await withTaskGroup(of: (String, Bool).self) { group in
            for resource in runnable {
                group.addTask {
                    let pinned = PinnedResource(
                        id: resource.id, name: resource.name, type: resource.type,
                        resourceGroup: resourceGroup, subscriptionId: subscriptionId,
                        location: resource.location, displayOrder: 0
                    )
                    let allowed = await self.permissionsService.canManage(resource: pinned)
                    return (resource.id, allowed)
                }
            }
            for await (id, allowed) in group {
                permissions[id] = allowed
            }
        }
    }

    func startApp(resource: AzureResource, rg: PinnedResourceGroup) async {
        let pinned = makePinnedResource(resource, rg: rg)
        appStates[resource.id] = .starting
        do {
            try await arm.startApp(resource: pinned)
            appStates[resource.id] = .running
        } catch {
            appStates[resource.id] = .stopped
        }
    }

    func stopApp(resource: AzureResource, rg: PinnedResourceGroup) async {
        let pinned = makePinnedResource(resource, rg: rg)
        appStates[resource.id] = .stopping
        do {
            try await arm.stopApp(resource: pinned)
            appStates[resource.id] = .stopped
        } catch {
            appStates[resource.id] = .running
        }
    }

    func restartApp(resource: AzureResource, rg: PinnedResourceGroup) async {
        let pinned = makePinnedResource(resource, rg: rg)
        appStates[resource.id] = .restarting
        do {
            try await arm.restartApp(resource: pinned)
            appStates[resource.id] = .running
        } catch {
            appStates[resource.id] = .running
        }
    }

    private func makePinnedResource(_ resource: AzureResource, rg: PinnedResourceGroup) -> PinnedResource {
        PinnedResource(
            id: resource.id, name: resource.name, type: resource.type,
            resourceGroup: rg.name, subscriptionId: rg.subscriptionId,
            location: resource.location, displayOrder: 0
        )
    }
}
