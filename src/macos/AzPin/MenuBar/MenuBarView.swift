import SwiftUI

#if DEBUG
final class MockAzCLIService: AzCLIServiceProtocol {
    func fetchToken(subscriptionId: String) async throws -> AzureTokenResponse {
        AzureTokenResponse(accessToken: "mock", expiresOn: .distantFuture)
    }
    
    func currentAccount() async throws -> AzureAccount {
        AzureAccount(id: "1", name: "Mock", tenantId: "1", user: AzureAccountUser(name: "mock@example.com"))
    }
    
    func listSubscriptions() async throws -> [AzureSubscription] {
        []
    }
    
    func isInstalled() -> Bool {
        true
    }
}
#endif

struct MenuBarView: View {
    @Environment(AuthViewModel.self) private var auth
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        Group {
            AuthStatusView()
            Divider()
            
            // Pinned resources will go here in MVP/Pre-release
            
            Button("Pin Resource Group...") {}
                .disabled(true)  // Placeholder — implemented in Pre-release
            
            Divider()
            
            Button("Open AzPin...") {
                openWindow(id: "main")
            }
            Button("Quit AzPin") {
                NSApplication.shared.terminate(nil)
            }
        }
        .task {
            await auth.refresh()
        }
    }
}

#Preview("Signed In") {
    let vm = AuthViewModel(azCLI: MockAzCLIService())
    // To show signedIn state, you'd need MockAzCLIService to return a valid account
    MenuBarView()
        .environment(vm)
}
