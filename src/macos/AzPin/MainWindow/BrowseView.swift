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
        .onChange(of: vm.selectedSubscriptionId) { _, newValue in
            guard newValue != nil else { return }
            Task { await vm.loadResourceGroups() }
        }
    }

    @ViewBuilder
    private var subscriptionPicker: some View {
        if vm.subscriptions.isEmpty && !vm.isLoadingSubscriptions {
            EmptyView()
        } else {
            Picker("Subscription", selection: Bindable(vm).selectedSubscriptionId) {
                Text("Select...").tag(Optional<String>.none)
                ForEach(vm.subscriptions, id: \.id) { sub in
                    Text(sub.name).tag(Optional(sub.id))
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
        } else if let _ = vm.selectedSubscriptionId {
            if vm.isLoadingResourceGroups {
                ProgressView("Loading resource groups...")
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else if vm.resourceGroups.isEmpty {
                ContentUnavailableView("No resource groups", systemImage: "folder")
            } else {
                List(vm.resourceGroups, id: \.id) { rg in
                    ResourceGroupRow(resourceGroup: rg)
                }
            }
        } else {
            ContentUnavailableView("Select a subscription above", systemImage: "arrow.up")
        }
    }
}
