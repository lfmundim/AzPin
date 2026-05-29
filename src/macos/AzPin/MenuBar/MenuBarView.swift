import SwiftUI

struct MenuBarView: View {
    var body: some View {
        Text("AzPin")
        Divider()
        Button("Open AzPin...") {}
        Divider()
        Button("Quit AzPin") {
            NSApplication.shared.terminate(nil)
        }
    }
}
