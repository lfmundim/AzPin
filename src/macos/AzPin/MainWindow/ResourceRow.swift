import SwiftUI
import SwiftData

struct ResourceRow: View {
    let resource: AzureResource
    let subscriptionId: String
    let resourceGroup: String
    let displayOrder: Int

    @Environment(\.modelContext) private var modelContext
    @State private var isPinned = false
    @State private var isHovering = false
    @Query private var pinnedRGs: [PinnedResourceGroup]

    init(resource: AzureResource, subscriptionId: String, resourceGroup: String, displayOrder: Int) {
        self.resource = resource
        self.subscriptionId = subscriptionId
        self.resourceGroup = resourceGroup
        self.displayOrder = displayOrder
        let sub = subscriptionId
        let rg = resourceGroup
        _pinnedRGs = Query(filter: #Predicate<PinnedResourceGroup> { $0.subscriptionId == sub && $0.name == rg })
    }

    private var isRGPinned: Bool { !pinnedRGs.isEmpty }

    var body: some View {
        HStack {
            Label {
                Text(resource.name).underline(isHovering)
            } icon: {
                Image(systemName: ResourceTypeMapper.symbolName(for: resource.type))
            }
            .contentShape(Rectangle())
            .onHover { hovering in
                isHovering = hovering
                if hovering { NSCursor.pointingHand.push() } else { NSCursor.pop() }
            }
            .onTapGesture {
                NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
            }
            Spacer()
            if !isRGPinned {
                Button {
                    isPinned ? unpinResource() : pinResource()
                } label: {
                    Image(systemName: isPinned ? "pin.fill" : "pin")
                }
                .buttonStyle(.plain)
                .foregroundStyle(isPinned ? Color.accentColor : .secondary)
            }
        }
        .onAppear {
            isPinned = checkIfPinned()
        }
        .contextMenu {
            Button {
                NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
            } label: {
                Label("Open in Portal", systemImage: "arrow.up.forward")
            }
            Button {
                NSPasteboard.general.clearContents()
                NSPasteboard.general.setString(resource.id, forType: .string)
            } label: {
                Label("Copy Resource ID", systemImage: "doc.on.doc")
            }
        }
    }

    private func pinResource() {
        let pinned = PinnedResource(
            id: resource.id,
            name: resource.name,
            type: resource.type,
            resourceGroup: resourceGroup,
            subscriptionId: subscriptionId,
            location: resource.location,
            displayOrder: displayOrder
        )
        modelContext.insert(pinned)
        isPinned = true
    }

    private func unpinResource() {
        let resourceId = resource.id
        let descriptor = FetchDescriptor<PinnedResource>(
            predicate: #Predicate { $0.id == resourceId }
        )
        if let results = try? modelContext.fetch(descriptor) {
            results.forEach { modelContext.delete($0) }
        }
        isPinned = false
    }

    private func checkIfPinned() -> Bool {
        let resourceId = resource.id
        let descriptor = FetchDescriptor<PinnedResource>(
            predicate: #Predicate { $0.id == resourceId }
        )
        guard let count = try? modelContext.fetchCount(descriptor) else { return false }
        return count > 0
    }

}
