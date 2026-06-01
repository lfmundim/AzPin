import SwiftUI

struct ResourceActionButtons: View {
    let state: AppRunningState
    let onStart: () -> Void
    let onStop: () -> Void
    let onRestart: () -> Void

    var body: some View {
        switch state {
        case .running:
            Button(action: onStop) {
                Image(systemName: "stop.fill").foregroundStyle(.red)
            }
            .buttonStyle(.plain)
            Button(action: onRestart) {
                Image(systemName: "arrow.clockwise")
            }
            .buttonStyle(.plain)

        case .stopped:
            Button(action: onStart) {
                Image(systemName: "play.fill").foregroundStyle(.green)
            }
            .buttonStyle(.plain)

        case .starting, .stopping, .restarting, .unknown:
            ProgressView().controlSize(.small)
        }
    }
}
