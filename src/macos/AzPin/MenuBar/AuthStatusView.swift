import SwiftUI

struct AuthStatusView: View {
    @Environment(AuthViewModel.self) private var auth

    var body: some View {
        switch auth.state {
        case .unknown:
            Label("Checking...", systemImage: "ellipsis")
                .foregroundStyle(.secondary)

        case .cliNotInstalled:
            VStack(alignment: .leading, spacing: 4) {
                Label("Azure CLI not installed", systemImage: "exclamationmark.triangle")
                    .foregroundStyle(.orange)
                Button("Install Azure CLI...") {
                    NSWorkspace.shared.open(URL(string: "https://aka.ms/installazureclimacos")!)
                }
                .font(.caption)
            }

        case .notSignedIn:
            Label("Not signed in — run 'az login'", systemImage: "exclamationmark.triangle")
                .foregroundStyle(.secondary)

        case .signedIn(let account):
            Label(account.user.name, systemImage: "checkmark.circle.fill")
                .foregroundStyle(.green)

        case .error(let message):
            Label(message, systemImage: "exclamationmark.triangle")
                .foregroundStyle(.red)
        }
    }
}
