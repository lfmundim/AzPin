import SwiftUI
import SwiftData

struct MenuBarView: View {
    @Environment(AuthViewModel.self) private var auth
    @Environment(MenuBarViewModel.self) private var menuVM
    @Environment(\.modelContext) private var modelContext
    @Environment(\.openWindow) private var openWindow
    @Environment(\.openSettings) private var openSettings
    @Query(sort: \PinnedResourceGroup.displayOrder) private var pinnedGroups: [PinnedResourceGroup]
    @Query(sort: \PinnedResource.displayOrder) private var pinnedResources: [PinnedResource]

    var body: some View {
        Group {
            AuthStatusView()
            Divider()

            ForEach(pinnedGroups) { rg in
                rgMenu(for: rg)
            }

            let orphans = pinnedResources.filter { resource in
                !pinnedGroups.contains(where: {
                    $0.name == resource.resourceGroup && $0.subscriptionId == resource.subscriptionId
                })
            }
            if !orphans.isEmpty {
                Divider()
                ForEach(orphans) { resource in
                    Button {
                        NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
                    } label: {
                        Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
                    }
                }
            }

            Divider()
            if pinnedGroups.isEmpty {
                Button("Pin Resource Group...") { openWindow(id: "main"); NSApp.activate(ignoringOtherApps: true) }
            }
            Button("Open AzPin...") { openWindow(id: "main"); NSApp.activate(ignoringOtherApps: true) }
            Button("Settings...") { openSettings() }
            Button("Quit AzPin") { NSApplication.shared.terminate(nil) }
        }
        .task {
            await auth.refresh()
            await menuVM.loadResources(for: pinnedGroups)
        }
        .onChange(of: pinnedGroups.map(\.id)) { _, _ in
            Task { await menuVM.loadResources(for: pinnedGroups) }
        }
    }

    @ViewBuilder
    private func rgMenu(for rg: PinnedResourceGroup) -> some View {
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
            Label(rg.name, systemImage: "folder.fill")
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
}
