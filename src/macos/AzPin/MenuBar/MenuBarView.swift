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

        Button {
            NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
        } label: {
            Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
        }

        if isRunnable && canManage {
            switch state {
            case .running:
                Button("Stop \(resource.name)") { Task { await menuVM.stopApp(resource: resource, rg: rg) } }
                Button("Restart \(resource.name)") { Task { await menuVM.restartApp(resource: resource, rg: rg) } }
            case .stopped:
                Button("Start \(resource.name)") { Task { await menuVM.startApp(resource: resource, rg: rg) } }
            case .starting, .stopping, .restarting, .unknown:
                Text("\(resource.name): updating...")
            }
        }
    }
}
