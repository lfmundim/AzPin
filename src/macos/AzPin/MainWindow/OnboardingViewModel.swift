import Foundation

@MainActor
@Observable
final class OnboardingViewModel {
    enum StepState { case pending, passed, failed(String) }

    private let azCLI: any AzCLIServiceProtocol
    var cliStep: StepState = .pending
    var signInStep: StepState = .pending
    var subscriptionStep: StepState = .pending
    var isPolling = false

    var allPassed: Bool {
        if case .passed = cliStep, case .passed = signInStep, case .passed = subscriptionStep { return true }
        return false
    }

    init(azCLI: any AzCLIServiceProtocol) {
        self.azCLI = azCLI
    }

    func startPolling() {
        isPolling = true
        Task {
            while isPolling {
                await checkAll()
                if allPassed { break }
                try? await Task.sleep(for: .seconds(2))
            }
        }
    }

    func stopPolling() {
        isPolling = false
    }

    private func checkAll() async {
        let cliInstalled = azCLI.isInstalled()
        cliStep = cliInstalled ? .passed : .failed("Install the Azure CLI from aka.ms/installazureclimacos")
        guard cliInstalled else { return }

        do {
            _ = try await azCLI.currentAccount()
            signInStep = .passed
        } catch {
            signInStep = .failed("Run 'az login' in your terminal")
            return
        }

        do {
            let subs = try await azCLI.listSubscriptions()
            subscriptionStep = subs.isEmpty
                ? .failed("Ensure your account has access to at least one Azure subscription")
                : .passed
        } catch {
            subscriptionStep = .failed(error.localizedDescription)
        }
    }
}
