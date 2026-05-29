import SwiftUI

struct AuthStatusView: View {
    var body: some View {
        Label("Not signed in — run 'az login'", systemImage: "exclamationmark.triangle")
            .foregroundStyle(.secondary)
    }
}
