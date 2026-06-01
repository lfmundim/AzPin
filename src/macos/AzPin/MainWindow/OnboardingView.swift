import SwiftUI

struct OnboardingView: View {
    @Environment(OnboardingViewModel.self) private var vm
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        VStack(alignment: .leading, spacing: 24) {
            Text("Welcome to AzPin")
                .font(.title2)
                .fontWeight(.semibold)

            Text("AzPin reads your existing Azure CLI session. Complete these steps to get started.")
                .foregroundStyle(.secondary)

            OnboardingStepRow(title: "Azure CLI installed", state: vm.cliStep)
            OnboardingStepRow(title: "Signed in to Azure", state: vm.signInStep)
            OnboardingStepRow(title: "Subscription accessible", state: vm.subscriptionStep)

            Spacer()

            Button("Get Started") {
                UserDefaults.standard.set(true, forKey: "hasCompletedOnboarding")
                vm.stopPolling()
                dismiss()
            }
            .buttonStyle(.borderedProminent)
            .disabled(!vm.allPassed)
            .frame(maxWidth: .infinity, alignment: .trailing)
        }
        .padding(32)
        .frame(width: 480, height: 320)
        .onAppear { vm.startPolling() }
        .onDisappear { vm.stopPolling() }
    }
}

struct OnboardingStepRow: View {
    let title: String
    let state: OnboardingViewModel.StepState

    var body: some View {
        HStack(spacing: 12) {
            stepIcon
            VStack(alignment: .leading) {
                Text(title)
                if case .failed(let msg) = state {
                    Text(msg).font(.caption).foregroundStyle(.secondary)
                }
            }
        }
    }

    @ViewBuilder
    private var stepIcon: some View {
        switch state {
        case .pending:
            Image(systemName: "circle").foregroundStyle(.secondary)
        case .passed:
            Image(systemName: "checkmark.circle.fill").foregroundStyle(.green)
        case .failed:
            Image(systemName: "exclamationmark.triangle.fill").foregroundStyle(.orange)
        }
    }
}
