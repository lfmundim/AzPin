import SwiftUI
import SwiftData

struct SidebarView: View {
    @Query(sort: \PinnedResourceGroup.displayOrder) private var pinnedGroups: [PinnedResourceGroup]
    @Binding var selectedGroup: PinnedResourceGroup?
    @Environment(\.modelContext) private var modelContext

    var body: some View {
        List(selection: $selectedGroup) {
            Section("Pinned") {
                ForEach(pinnedGroups) { rg in
                    Label(rg.name, systemImage: "folder.fill")
                        .tag(rg)
                        .contextMenu {
                            Button(role: .destructive) {
                                modelContext.delete(rg)
                            } label: {
                                Label("Unpin", systemImage: "pin.slash")
                            }
                        }
                }
                .onMove(perform: reorder)
            }
        }
        .navigationTitle("AzPin")
        .listStyle(.sidebar)
    }

    private func reorder(from source: IndexSet, to destination: Int) {
        var reordered = pinnedGroups
        reordered.move(fromOffsets: source, toOffset: destination)
        for (index, rg) in reordered.enumerated() {
            rg.displayOrder = index
        }
    }
}
