import SwiftUI

struct RGBrowseView: View {
    let resourceGroup: PinnedResourceGroup
    @Environment(RGBrowseViewModel.self) private var vm

    var body: some View {
        Group {
            if vm.isLoading {
                ProgressView("Loading resources...")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else if let error = vm.errorMessage {
                ContentUnavailableView(error, systemImage: "exclamationmark.triangle")
            } else if vm.resources.isEmpty {
                ContentUnavailableView("No resources", systemImage: "cube.box")
            } else {
                ScrollView {
                    LazyVStack(alignment: .leading, spacing: 0) {
                        ForEach(Array(vm.resources.enumerated()), id: \.element.id) { index, resource in
                            ResourceRow(
                                resource: resource,
                                subscriptionId: resourceGroup.subscriptionId,
                                resourceGroup: resourceGroup.name,
                                displayOrder: index
                            )
                            .padding(.horizontal, 8)
                            Divider()
                        }
                    }
                }
            }
        }
        .task(id: resourceGroup.id) {
            await vm.loadResources(
                subscriptionId: resourceGroup.subscriptionId,
                rgName: resourceGroup.name,
                tenantId: ""
            )
        }
    }
}
