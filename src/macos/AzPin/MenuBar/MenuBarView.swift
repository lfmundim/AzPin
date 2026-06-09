import SwiftUI
import SwiftData

struct MenuBarView: View {
    @Environment(AuthViewModel.self) private var auth
    @Environment(MenuBarViewModel.self) private var menuVM
    @Environment(UpdateCheckService.self) private var updateChecker
    @Environment(\.modelContext) private var modelContext
    @Environment(\.openWindow) private var openWindow
    @Environment(\.openSettings) private var openSettings
    @Query(sort: \PinnedResourceGroup.displayOrder) private var pinnedGroups: [PinnedResourceGroup]
    @Query(sort: \PinnedResource.displayOrder) private var pinnedResources: [PinnedResource]

    var body: some View {
        Group {
            AuthStatusView()
            Divider()

            let duplicateRGNames: Set<String> = {
                var counts: [String: Int] = [:]
                for rg in pinnedGroups { counts[rg.name, default: 0] += 1 }
                return Set(counts.filter { $0.value > 1 }.keys)
            }()
            ForEach(pinnedGroups) { rg in
                rgMenu(for: rg, showSubscription: duplicateRGNames.contains(rg.name))
            }

            let orphans = pinnedResources.filter { resource in
                !pinnedGroups.contains(where: {
                    $0.name == resource.resourceGroup && $0.subscriptionId == resource.subscriptionId
                })
            }
            if !orphans.isEmpty {
                Divider()
                ForEach(orphans) { resource in
                    orphanResourceItems(resource: resource)
                }
            }

            Divider()
            if pinnedGroups.isEmpty {
                Button("Pin Resource Group...") { openMainWindow() }
            }
            Button("Open AzPin...") { openMainWindow() }
            if case .updateAvailable(_, let latest, let url) = updateChecker.state {
                Button("Update Available: v\(latest)") {
                    NSWorkspace.shared.open(url)
                }
            }
            Button("Settings...") { openSettings(); NSApp.activate(ignoringOtherApps: true) }
            Button("Quit AzPin") { NSApplication.shared.terminate(nil) }
        }
        .task {
            await auth.refresh()
            await menuVM.loadResources(for: pinnedGroups)
            await menuVM.loadOrphanResources(for: computeOrphans(resources: pinnedResources, groups: pinnedGroups))
            if !UserDefaults.standard.bool(forKey: "hasCompletedOnboarding") {
                openMainWindow()
            }
        }
        .onChange(of: pinnedGroups.map(\.id)) { _, _ in
            Task { await menuVM.loadResources(for: pinnedGroups) }
        }
        .onChange(of: pinnedResources.map(\.id)) { _, _ in
            let orphans = computeOrphans(resources: pinnedResources, groups: pinnedGroups)
            Task { await menuVM.loadOrphanResources(for: orphans) }
        }
    }

    @ViewBuilder
    private func rgMenu(for rg: PinnedResourceGroup, showSubscription: Bool = false) -> some View {
        let subName = rg.subscriptionDisplayName ?? ""
        let label = showSubscription && !subName.isEmpty
            ? "\(rg.name) · \(subName)"
            : rg.name
        Menu {
            if let error = menuVM.loadErrors[rg.id] {
                Label(error, systemImage: "exclamationmark.triangle")
            } else if let resources = menuVM.resourcesByRG[rg.id] {
                // nil = not yet fetched; empty array = fetched but group has no resources
                if resources.isEmpty {
                    Text("No resources in this group")
                } else {
                    ForEach(resources, id: \.id) { resource in
                        resourceItems(resource: resource, rg: rg)
                    }
                }
            } else {
                Text("Loading...")
            }

            Divider()

            Button {
                NSWorkspace.shared.open(PortalURL.resourceGroup(subscriptionId: rg.subscriptionId, name: rg.name))
            } label: {
                Label("Open Resource Group in Portal", systemImage: "arrow.up.forward")
            }

            Divider()

            Button(role: .destructive) {
                modelContext.delete(rg)
            } label: {
                Label("Unpin", systemImage: "pin.slash")
            }
        } label: {
            Label(label, systemImage: "folder.fill")
        }
    }

    @ViewBuilder
    private func resourceItems(resource: AzureResource, rg: PinnedResourceGroup) -> some View {
        let isRunnable = ResourceTypeMapper.isRunnable(resource.type)
        let state = menuVM.appStates[resource.id] ?? .unknown
        let canManage = menuVM.permissions[resource.id] == true

        if isRunnable && canManage {
            Menu {
                actionButtons(state: state, resource: resource, rg: rg)
                Divider()
                Button {
                    NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
                } label: {
                    Label("Open in Portal", systemImage: "arrow.up.forward")
                }
            } label: {
                Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
            }
        } else {
            Button {
                NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
            } label: {
                Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
            }
        }
    }

    @ViewBuilder
    private func actionButtons(state: AppRunningState, resource: AzureResource, rg: PinnedResourceGroup) -> some View {
        switch state {
        case .running:
            Button {
                Task { await menuVM.stopApp(resource: resource, rg: rg) }
            } label: {
                Label("Stop", systemImage: "stop.fill")
            }
            Button {
                Task { await menuVM.restartApp(resource: resource, rg: rg) }
            } label: {
                Label("Restart", systemImage: "arrow.clockwise")
            }
        case .stopped:
            Button {
                Task { await menuVM.startApp(resource: resource, rg: rg) }
            } label: {
                Label("Start", systemImage: "play.fill")
            }
        case .starting, .stopping, .restarting, .unknown:
            Text("Updating...")
        }
    }

    @ViewBuilder
    private func orphanResourceItems(resource: PinnedResource) -> some View {
        let isRunnable = ResourceTypeMapper.isRunnable(resource.type)
        let state = menuVM.appStates[resource.id] ?? .unknown
        let canManage = menuVM.permissions[resource.id] == true

        if isRunnable && canManage {
            Menu {
                orphanActionButtons(state: state, resource: resource)
                Divider()
                Button {
                    NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
                } label: {
                    Label("Open in Portal", systemImage: "arrow.up.forward")
                }
            } label: {
                Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
            }
        } else {
            Button {
                NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
            } label: {
                Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
            }
        }
    }

    private func openMainWindow() {
        NSApplication.shared.setActivationPolicy(.regular)
        openWindow(id: "main")
        NSApp.activate(ignoringOtherApps: true)
    }

    private func computeOrphans(resources: [PinnedResource], groups: [PinnedResourceGroup]) -> [PinnedResource] {
        resources.filter { r in
            !groups.contains(where: { g in g.name == r.resourceGroup && g.subscriptionId == r.subscriptionId })
        }
    }

    @ViewBuilder
    private func orphanActionButtons(state: AppRunningState, resource: PinnedResource) -> some View {
        switch state {
        case .running:
            Button {
                Task { await menuVM.stopApp(resource: resource) }
            } label: {
                Label("Stop", systemImage: "stop.fill")
            }
            Button {
                Task { await menuVM.restartApp(resource: resource) }
            } label: {
                Label("Restart", systemImage: "arrow.clockwise")
            }
        case .stopped:
            Button {
                Task { await menuVM.startApp(resource: resource) }
            } label: {
                Label("Start", systemImage: "play.fill")
            }
        case .starting, .stopping, .restarting, .unknown:
            Text("Updating...")
        }
    }
}
