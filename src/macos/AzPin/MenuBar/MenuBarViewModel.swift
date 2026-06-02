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

    func loadOrphanResources(for resources: [PinnedResource]) async {
        let snapshots = resources
            .filter { ResourceTypeMapper.isRunnable($0.type) }
            .map { snapshotPinnedResource($0) }
        guard !snapshots.isEmpty else { return }
        await withTaskGroup(of: Void.self) { group in
            group.addTask { await self.fetchOrphanStates(for: snapshots) }
            group.addTask { await self.checkOrphanPermissions(for: snapshots) }
            await group.waitForAll()
        }
    }

    private func fetchOrphanStates(for snapshots: [PinnedResource]) async {
        await withTaskGroup(of: (String, AppRunningState).self) { group in
            for snap in snapshots {
                group.addTask {
                    let state = (try? await self.arm.fetchAppState(resource: snap)) ?? .unknown
                    return (snap.id, state)
                }
            }
            for await (id, state) in group {
                appStates[id] = state
            }
        }
    }

    private func checkOrphanPermissions(for snapshots: [PinnedResource]) async {
        await withTaskGroup(of: (String, Bool).self) { group in
            for snap in snapshots {
                group.addTask {
                    let allowed = await self.permissionsService.canManage(resource: snap)
                    return (snap.id, allowed)
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

    func startApp(resource: PinnedResource) async {
        let snap = snapshotPinnedResource(resource)
        appStates[snap.id] = .starting
        do {
            try await arm.startApp(resource: snap)
            appStates[snap.id] = .running
        } catch {
            appStates[snap.id] = .stopped
        }
    }

    func stopApp(resource: PinnedResource) async {
        let snap = snapshotPinnedResource(resource)
        appStates[snap.id] = .stopping
        do {
            try await arm.stopApp(resource: snap)
            appStates[snap.id] = .stopped
        } catch {
            appStates[snap.id] = .running
        }
    }

    func restartApp(resource: PinnedResource) async {
        let snap = snapshotPinnedResource(resource)
        appStates[snap.id] = .restarting
        do {
            try await arm.restartApp(resource: snap)
            appStates[snap.id] = .running
        } catch {
            appStates[snap.id] = .running
        }
    }

    private func makePinnedResource(_ resource: AzureResource, rg: PinnedResourceGroup) -> PinnedResource {
        PinnedResource(
            id: resource.id, name: resource.name, type: resource.type,
            resourceGroup: rg.name, subscriptionId: rg.subscriptionId,
            location: resource.location, displayOrder: 0
        )
    }

    // Creates an unmanaged (not SwiftData-context-bound) copy safe to cross actor/task boundaries.
    private func snapshotPinnedResource(_ resource: PinnedResource) -> PinnedResource {
        PinnedResource(
            id: resource.id, name: resource.name, type: resource.type,
            resourceGroup: resource.resourceGroup, subscriptionId: resource.subscriptionId,
            location: resource.location, displayOrder: 0
        )
    }
}
