import SwiftUI
import SwiftData

struct PinnedResourcesView: View {
    let resourceGroup: PinnedResourceGroup
    @Query private var pinnedResources: [PinnedResource]
    @Environment(\.modelContext) private var modelContext

    init(resourceGroup: PinnedResourceGroup) {
        self.resourceGroup = resourceGroup
        let rgName = resourceGroup.name
        let subId = resourceGroup.subscriptionId
        _pinnedResources = Query(
            filter: #Predicate<PinnedResource> { r in
                r.resourceGroup == rgName && r.subscriptionId == subId
            },
            sort: \PinnedResource.displayOrder
        )
    }

    var body: some View {
        if pinnedResources.isEmpty {
            ContentUnavailableView(
                "No Pinned Resources",
                systemImage: "pin",
                description: Text("Switch to Browse to pin individual resources.")
            )
        } else {
            List {
                ForEach(pinnedResources) { resource in
                    PinnedResourceRow(resource: resource)
                }
                .onMove(perform: reorder)
                .onDelete(perform: unpin)
            }
        }
    }

    private func reorder(from source: IndexSet, to destination: Int) {
        var reordered = pinnedResources
        reordered.move(fromOffsets: source, toOffset: destination)
        for (index, r) in reordered.enumerated() {
            r.displayOrder = index
        }
    }

    private func unpin(at offsets: IndexSet) {
        for index in offsets {
            modelContext.delete(pinnedResources[index])
        }
    }
}
