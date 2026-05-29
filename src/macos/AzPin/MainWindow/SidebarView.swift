import SwiftUI

struct SidebarView: View {
    var body: some View {
        List {
            Text("Pinned Resource Groups")
                .foregroundStyle(.secondary)
                .font(.caption)
        }
        .navigationTitle("AzPin")
    }
}
