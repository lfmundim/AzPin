import SwiftUI
import SwiftData

struct PinnedResourceRow: View {
    let resource: PinnedResource
    @Environment(\.modelContext) private var modelContext

    var body: some View {
        HStack {
            Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
            Spacer()
            Button {
                NSWorkspace.shared.open(PortalURL.resource(id: resource.id))
            } label: {
                Image(systemName: "arrow.up.forward")
            }
            .buttonStyle(.plain)
            .foregroundStyle(.secondary)
            Button {
                modelContext.delete(resource)
            } label: {
                Image(systemName: "pin.fill")
            }
            .buttonStyle(.plain)
            .foregroundStyle(Color.accentColor)
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
            Divider()
            Button(role: .destructive) {
                modelContext.delete(resource)
            } label: {
                Label("Unpin", systemImage: "pin.slash")
            }
        }
    }
}
