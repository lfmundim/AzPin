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
    let browseViewModel: BrowseViewModel
    let onboardingViewModel: OnboardingViewModel
    let menuBarViewModel: MenuBarViewModel
    let accountSettingsViewModel: AccountSettingsViewModel

    init() {
        let c = try! ModelContainer(for: PinnedResourceGroup.self, PinnedResource.self, CachedToken.self)
        let az = AzCLIService()
        let tc = TokenCache(modelContext: c.mainContext, azCLI: az)
        let arm = ARMService(tokenCache: tc)
        container = c
        azCli = az
        tokenCache = tc
        self.arm = arm
        permissions = PermissionsService(tokenCache: tc)
        authViewModel = AuthViewModel(azCLI: az)
        browseViewModel = BrowseViewModel(azCLI: az, arm: arm)
        onboardingViewModel = OnboardingViewModel(azCLI: az)
        menuBarViewModel = MenuBarViewModel(arm: arm, permissionsService: permissions)
        accountSettingsViewModel = AccountSettingsViewModel(azCLI: az, tokenCache: tc)
    }

    var body: some Scene {
        MenuBarExtra("AzPin", systemImage: "cloud.fill") {
            MenuBarView()
                .environment(azCli)
                .environment(tokenCache)
                .environment(arm)
                .environment(permissions)
                .environment(authViewModel)
                .environment(menuBarViewModel)
                .environment(\.modelContext, container.mainContext)
        }
        .menuBarExtraStyle(.menu)
        .modelContainer(container)

        Window("AzPin", id: "main") {
            MainAppView()
                .environment(azCli)
                .environment(tokenCache)
                .environment(arm)
                .environment(permissions)
                .environment(authViewModel)
                .environment(browseViewModel)
                .environment(onboardingViewModel)
        }
        .windowStyle(.titleBar)
        .defaultSize(width: 900, height: 600)
        .modelContainer(container)

        Settings {
            SettingsView()
                .environment(azCli)
                .environment(tokenCache)
                .environment(arm)
                .environment(permissions)
                .environment(authViewModel)
                .environment(accountSettingsViewModel)
        }
        .modelContainer(container)
    }
}
