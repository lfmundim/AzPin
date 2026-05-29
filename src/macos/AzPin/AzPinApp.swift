import SwiftUI
import SwiftData

@main
struct AzPinApp: App {
    var body: some Scene {
        MenuBarExtra("AzPin", systemImage: "cloud.fill") {
            MenuBarView()
        }
        .menuBarExtraStyle(.menu)

        Window("AzPin", id: "main") {
            MainAppView()
        }
        .windowStyle(.titleBar)
        .defaultSize(width: 900, height: 600)

        Settings {
            SettingsView()
        }
    }
}
