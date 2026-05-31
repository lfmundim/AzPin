import SwiftUI
import SwiftData

@main
struct AzPinApp: App {
    let container: ModelContainer
    let azCli: AzCLIService
    let tokenCache: TokenCache
    let arm: ARMService
    let permissions: PermissionsService
    let authViewModel: AuthViewModel
    
    init() {
        let c = try! ModelContainer(for: PinnedResourceGroup.self, PinnedResource.self, CachedToken.self)
        let az = AzCLIService()
        let tc = TokenCache(modelContext: c.mainContext, azCLI: az)
        container = c
        azCli = az
        tokenCache = tc
        arm = ARMService(tokenCache: tc)
        permissions = PermissionsService(tokenCache: tc)
        authViewModel = AuthViewModel(azCLI: az)
    }
    
    var body: some Scene {
        MenuBarExtra("AzPin", systemImage: "cloud.fill") {
            MenuBarView()
                .environment(azCli)
                .environment(tokenCache)
                .environment(arm)
                .environment(permissions)
                .environment(authViewModel)
        }
        .menuBarExtraStyle(.menu)

        Window("AzPin", id: "main") {
            MainAppView()
                .environment(azCli)
                .environment(tokenCache)
                .environment(arm)
                .environment(permissions)
                .environment(authViewModel)
        }
        .windowStyle(.titleBar)
        .defaultSize(width: 900, height: 600)

        Settings {
            SettingsView()
                .environment(azCli)
                .environment(tokenCache)
                .environment(arm)
                .environment(permissions)
                .environment(authViewModel)
        }
    }
}
