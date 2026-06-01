import SwiftUI
import SwiftData

struct ResourceGroupRow: View {
    let resourceGroup: AzureResourceGroup
    let subscriptionId: String
    let displayOrder: Int

    @Environment(\.modelContext) private var modelContext
    @State private var isPinned = false

    var body: some View {
        HStack {
            Label(resourceGroup.name, systemImage: "folder.fill")
            Spacer()
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
            displayOrder: displayOrder
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
