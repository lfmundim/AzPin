import SwiftUI

struct SettingsView: View {
    var body: some View {
        TabView {
            AccountSettingsView()
                .tabItem { Label("Account", systemImage: "person.crop.circle") }
            Text("Subscriptions — coming in v1.1")
                .tabItem { Label("Subscriptions", systemImage: "list.bullet") }
            Text("Background polling — off in v1")
                .tabItem { Label("Preferences", systemImage: "gearshape") }
        }
        .frame(width: 480, height: 360)
    }
}
