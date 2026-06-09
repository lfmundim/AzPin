import SwiftUI
import ServiceManagement

struct PreferencesSettingsView: View {
    @Environment(UpdateCheckService.self) private var updateChecker
    @State private var launchAtLogin = false

    private var currentVersion: String {
        Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "—"
    }

    var body: some View {
        Form {
            Section {
                Toggle("Open at Login", isOn: $launchAtLogin)
                    .onChange(of: launchAtLogin) { _, enabled in
                        do {
                            if enabled {
                                try SMAppService.mainApp.register()
                            } else {
                                try SMAppService.mainApp.unregister()
                            }
                        } catch {
                            launchAtLogin = !enabled
                        }
                    }
            }

            Section("Updates") {
                LabeledContent("Current Version", value: currentVersion)
                updateCheckRow
            }
        }
        .formStyle(.grouped)
        .onAppear {
            launchAtLogin = SMAppService.mainApp.status == .enabled
        }
    }

    @ViewBuilder
    private var updateCheckRow: some View {
        switch updateChecker.state {
        case .idle:
            Button("Check for Updates") {
                Task { await updateChecker.checkForUpdates() }
            }

        case .checking:
            HStack {
                ProgressView()
                    .scaleEffect(0.7)
                Text("Checking...")
                    .foregroundStyle(.secondary)
            }

        case .upToDate(let version):
            HStack {
                Image(systemName: "checkmark.circle.fill")
                    .foregroundStyle(Color.green)
                Text("v\(version) is the latest")
                    .foregroundStyle(.secondary)
                Spacer()
                Button("Check Again") {
                    Task { await updateChecker.checkForUpdates() }
                }
                .buttonStyle(.plain)
                .foregroundStyle(Color.accentColor)
            }

        case .updateAvailable(let current, let latest, let url):
            VStack(alignment: .leading, spacing: 8) {
                HStack {
                    Image(systemName: "arrow.down.circle.fill")
                        .foregroundStyle(Color.accentColor)
                    Text("v\(latest) available — you have v\(current)")
                        .fontWeight(.medium)
                }
                Text("brew upgrade azpin")
                    .font(.system(.body, design: .monospaced))
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2)
                    .background(Color.secondary.opacity(0.15))
                    .clipShape(RoundedRectangle(cornerRadius: 4))
                Link("View release on GitHub", destination: url)
                    .font(.callout)
            }
            .padding(.vertical, 4)

        case .failed(let message):
            HStack {
                Image(systemName: "exclamationmark.triangle.fill")
                    .foregroundStyle(Color.red)
                Text(message)
                    .foregroundStyle(.secondary)
                    .lineLimit(2)
                Spacer()
                Button("Retry") {
                    Task { await updateChecker.checkForUpdates() }
                }
                .buttonStyle(.plain)
                .foregroundStyle(Color.accentColor)
            }
        }
    }
}
