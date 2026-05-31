import SwiftUI
import SwiftData

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
    @Query(sort: \PinnedResource.displayOrder) private var pinnedResources: [PinnedResource]

    var body: some View {
        Group {
            AuthStatusView()
            Divider()

            if !pinnedResources.isEmpty {
                ForEach(pinnedResources) { resource in
                    Button {
                        openInPortal(resource)
                    } label: {
                        ResourceMenuItem(
                            name: resource.name,
                            symbolName: ResourceTypeMapper.symbolName(for: resource.type)
                        )
                    }
                }
                Divider()
            }

            Button("Pin Resource Group...") {}
                .disabled(true)

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

    private func openInPortal(_ resource: PinnedResource) {
        let url = PortalURL.resource(id: resource.id)
        NSWorkspace.shared.open(url)
    }
}

#Preview("Signed In") {
    let vm = AuthViewModel(azCLI: MockAzCLIService())
    MenuBarView()
        .environment(vm)
}
