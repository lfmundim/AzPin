import SwiftUI

struct BrowseView: View {
    @Environment(BrowseViewModel.self) private var vm

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            subscriptionPicker
            Divider()
            contentArea
        }
        .task {
            await vm.loadSubscriptions()
        }
        .onChange(of: vm.selectedSubscription) { _, newValue in
            guard newValue != nil else { return }
            Task { await vm.loadResourceGroups() }
        }
    }

    @ViewBuilder
    private var subscriptionPicker: some View {
        if vm.subscriptions.isEmpty && !vm.isLoadingSubscriptions {
            EmptyView()
        } else {
            Picker("Subscription", selection: Bindable(vm).selectedSubscription) {
                Text("Select...").tag(Optional<AzureSubscription>.none)
                ForEach(vm.subscriptions, id: \.id) { sub in
                    Text(sub.name).tag(Optional(sub))
                }
            }
            .padding()
        }
    }

    @ViewBuilder
    private var contentArea: some View {
        if vm.isLoadingSubscriptions {
            ProgressView("Loading subscriptions...")
                .frame(maxWidth: .infinity, maxHeight: .infinity)
        } else if let error = vm.errorMessage {
            ContentUnavailableView(error, systemImage: "exclamationmark.triangle")
        } else if vm.subscriptions.isEmpty {
            ContentUnavailableView(
                "No subscriptions found",
                systemImage: "list.bullet",
                description: Text("Ensure your account has access to at least one Azure subscription.")
            )
        } else if let _ = vm.selectedSubscription {
            if vm.isLoadingResourceGroups {
                ProgressView("Loading resource groups...")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else if vm.resourceGroups.isEmpty {
                ContentUnavailableView("No resource groups", systemImage: "folder")
            } else {
                List(Array(vm.resourceGroups.enumerated()), id: \.element.id) { index, rg in
                    VStack(alignment: .leading, spacing: 0) {
                        ResourceGroupRow(
                            resourceGroup: rg,
                            subscriptionId: vm.selectedSubscription?.id ?? "",
                            displayOrder: index
                        )
                        .onTapGesture {
                                if vm.selectedResourceGroupName == rg.name {
                                    vm.selectedResourceGroupName = nil
                                } else {
                                    vm.selectedResourceGroupName = rg.name
                                    Task { await vm.loadResources(in: rg.name) }
                                }
                            }

                        if vm.selectedResourceGroupName == rg.name {
                            if vm.isLoadingResources {
                                ProgressView()
                                    .padding(.leading, 24)
                            } else {
                                ForEach(Array(vm.resources.enumerated()), id: \.element.id) { index, resource in
                                    ResourceRow(
                                        resource: resource,
                                        subscriptionId: vm.selectedSubscription?.id ?? "",
                                        resourceGroup: vm.selectedResourceGroupName ?? "",
                                        displayOrder: index
                                    )
                                    .padding(.leading, 24)
                                }
                            }
                        }
                    }
                }
            }
        } else {
            ContentUnavailableView("Select a subscription above", systemImage: "arrow.up")
        }
    }
}
