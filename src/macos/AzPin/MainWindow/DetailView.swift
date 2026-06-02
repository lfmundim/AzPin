import SwiftUI

struct DetailView: View {
    let selectedGroup: PinnedResourceGroup?

    var body: some View {
        if let group = selectedGroup {
            TabView {
                PinnedResourcesView(resourceGroup: group)
                    .tabItem { Label("Pinned", systemImage: "pin.fill") }
                RGBrowseView(resourceGroup: group)
                    .tabItem { Label("Browse", systemImage: "magnifyingglass") }
            }
        } else {
            BrowseView()
        }
    }
}
