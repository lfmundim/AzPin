import SwiftUI

struct SubscriptionSettingsView: View {
    @Environment(AccountSettingsViewModel.self) private var vm
    @AppStorage("hiddenSubscriptionIds") private var hiddenIdsRaw: String = ""

    private var hiddenIds: Set<String> {
        Set(hiddenIdsRaw.split(separator: ",").map(String.init).filter { !$0.isEmpty })
    }

    var body: some View {
        Form {
            if vm.isLoading {
                ProgressView("Loading subscriptions...")
            } else if let error = vm.errorMessage {
                Label(error, systemImage: "exclamationmark.triangle")
                    .foregroundStyle(.red)
            } else if vm.subscriptions.isEmpty {
                ContentUnavailableView("No subscriptions found", systemImage: "list.bullet")
            } else {
                Section {
                    ForEach(vm.subscriptions, id: \.id) { sub in
                        Toggle(sub.name, isOn: Binding(
                            get: { !hiddenIds.contains(sub.id) },
                            set: { isVisible in
                                var ids = hiddenIds
                                if isVisible { ids.remove(sub.id) } else { ids.insert(sub.id) }
                                hiddenIdsRaw = ids.joined(separator: ",")
                            }
                        ))
                    }
                } footer: {
                    Text("Hidden subscriptions won't appear in Browse. Changes apply on the next load.")
                        .foregroundStyle(.secondary)
                }
            }
        }
        .formStyle(.grouped)
        .task { await vm.load() }
    }
}
