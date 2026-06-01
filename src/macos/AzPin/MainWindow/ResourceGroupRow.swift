import SwiftUI
import SwiftData

struct ResourceGroupRow: View {
    let resourceGroup: AzureResourceGroup
    let subscriptionId: String
    let subscriptionName: String
    let displayOrder: Int
    var isExpanded: Bool = false
    var onToggle: (() -> Void)? = nil

    @Environment(\.modelContext) private var modelContext
    @State private var isPinned = false
    @State private var isHovering = false

    var body: some View {
        HStack {
            Label {
                Text(resourceGroup.name).underline(isHovering)
            } icon: {
                Image(systemName: "folder.fill")
            }
            .contentShape(Rectangle())
            .onHover { hovering in
                isHovering = hovering
                if hovering { NSCursor.pointingHand.push() } else { NSCursor.pop() }
            }
            .onTapGesture {
                NSWorkspace.shared.open(PortalURL.resourceGroup(subscriptionId: subscriptionId, name: resourceGroup.name))
            }
            Spacer()
            Button {
                onToggle?()
            } label: {
                Image(systemName: "chevron.right")
                    .rotationEffect(.degrees(isExpanded ? 90 : 0))
                    .animation(.easeInOut(duration: 0.2), value: isExpanded)
            }
            .buttonStyle(.plain)
            .foregroundStyle(.secondary)
            Button {
                isPinned ? unpinResourceGroup() : pinResourceGroup()
            } label: {
                Image(systemName: isPinned ? "pin.fill" : "pin")
            }
            .buttonStyle(.plain)
            .foregroundStyle(isPinned ? Color.accentColor : .secondary)
        }
        .onAppear {
            isPinned = checkIfPinned()
        }
    }

    private func pinResourceGroup() {
        let pinned = PinnedResourceGroup(
            id: resourceGroup.id,
            subscriptionId: subscriptionId,
            name: resourceGroup.name,
            displayOrder: displayOrder,
            subscriptionDisplayName: subscriptionName.isEmpty ? nil : subscriptionName
        )
        modelContext.insert(pinned)
        isPinned = true
    }

    private func unpinResourceGroup() {
        let id = resourceGroup.id
        let descriptor = FetchDescriptor<PinnedResourceGroup>(
            predicate: #Predicate { $0.id == id }
        )
        if let results = try? modelContext.fetch(descriptor) {
            results.forEach { modelContext.delete($0) }
        }
        isPinned = false
    }

    private func checkIfPinned() -> Bool {
        let id = resourceGroup.id
        let descriptor = FetchDescriptor<PinnedResourceGroup>(
            predicate: #Predicate { $0.id == id }
        )
        guard let count = try? modelContext.fetchCount(descriptor) else { return false }
        return count > 0
    }
}
