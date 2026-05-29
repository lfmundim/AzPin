import SwiftUI

struct SettingsView: View {
    var body: some View {
        TabView {
            Text("Account")
                .tabItem { Label("Account", systemImage: "person.crop.circle") }
            Text("Subscriptions")
                .tabItem { Label("Subscriptions", systemImage: "list.bullet") }
            Text("Preferences")
                .tabItem { Label("Preferences", systemImage: "gearshape") }
        }
        .frame(width: 480, height: 320)
    }
}
