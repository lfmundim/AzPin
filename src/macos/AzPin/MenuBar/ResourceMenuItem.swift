import SwiftUI

struct ResourceMenuItem: View {
    let name: String
    let symbolName: String
    var state: AppRunningState? = nil

    var body: some View {
        HStack {
            Label(name, systemImage: symbolName)
            Spacer()
            if let state {
                stateIndicator(for: state)
            }
        }
    }

    @ViewBuilder
    private func stateIndicator(for state: AppRunningState) -> some View {
        switch state {
        case .running:
            Image(systemName: "circle.fill").foregroundStyle(.green).imageScale(.small)
        case .stopped:
            Image(systemName: "circle.fill").foregroundStyle(.red).imageScale(.small)
        case .starting, .stopping, .restarting, .unknown:
            ProgressView().controlSize(.small)
        }
    }
}
