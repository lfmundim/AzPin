import SwiftUI

struct SettingsView: View {
    var body: some View {
        TabView {
            AccountSettingsView()
                .tabItem { Label("Account", systemImage: "person.crop.circle") }
            SubscriptionSettingsView()
                .tabItem { Label("Subscriptions", systemImage: "list.bullet") }
            PreferencesSettingsView()
                .tabItem { Label("Preferences", systemImage: "gearshape") }
        }
        .frame(width: 480, height: 360)
    }
}
