import SwiftUI

@MainActor
@Observable
final class AccountSettingsViewModel {
    private let azCLI: any AzCLIServiceProtocol
    private let tokenCache: any TokenCacheProtocol
    var account: AzureAccount?
    var subscriptions: [AzureSubscription] = []
    var isLoading = false
    var errorMessage: String?

    init(azCLI: any AzCLIServiceProtocol, tokenCache: any TokenCacheProtocol) {
        self.azCLI = azCLI
        self.tokenCache = tokenCache
    }

    func load() async {
        isLoading = true
        errorMessage = nil
        defer { isLoading = false }
        do {
            async let account = azCLI.currentAccount()
            async let subs = azCLI.listSubscriptions()
            self.account = try await account
            self.subscriptions = try await subs
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func refreshToken() async {
        for sub in subscriptions {
            await tokenCache.invalidate(subscriptionId: sub.id)
        }
        await load()
    }
}

struct AccountSettingsView: View {
    @Environment(AccountSettingsViewModel.self) private var vm

    var body: some View {
        Form {
            if vm.isLoading {
                ProgressView("Loading account info...")
            } else if let error = vm.errorMessage {
                Label(error, systemImage: "exclamationmark.triangle")
                    .foregroundStyle(.red)
            } else if let account = vm.account {
                Section("Identity") {
                    LabeledContent("User", value: account.user.name)
                    LabeledContent("Tenant ID", value: account.tenantId)
                }
                Section("Active Subscription") {
                    LabeledContent("Name", value: account.name)
                    LabeledContent("ID", value: account.id)
                }
            }

            Section {
                Button("Refresh Token") {
                    Task { await vm.refreshToken() }
                }
                Button("Re-run setup") {
                    UserDefaults.standard.removeObject(forKey: "hasCompletedOnboarding")
                }
            }
        }
        .formStyle(.grouped)
        .task { await vm.load() }
    }
}
