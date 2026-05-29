import Foundation

protocol AzCLIServiceProtocol: Sendable {
    func fetchToken(subscriptionId: String) async throws -> AzureTokenResponse
    func currentAccount() async throws -> AzureAccount
    func listSubscriptions() async throws -> [AzureSubscription]
    func isInstalled() -> Bool
}

@Observable
final class AzCLIService: AzCLIServiceProtocol {
    private let shell: ShellRunner

    init(shell: ShellRunner = ShellRunner()) {
        self.shell = shell
    }

    func fetchToken(subscriptionId: String) async throws -> AzureTokenResponse {
        let json = try await shell.run("\(resolveAzPath()) account get-access-token --subscription \(subscriptionId) --output json")
        return try JSONDecoder().decode(AzureTokenResponse.self, from: Data(json.utf8))
    }

    func currentAccount() async throws -> AzureAccount {
        let json = try await shell.run("\(resolveAzPath()) account show --output json")
        return try JSONDecoder().decode(AzureAccount.self, from: Data(json.utf8))
    }

    func listSubscriptions() async throws -> [AzureSubscription] {
        let json = try await shell.run("\(resolveAzPath()) account list --output json")
        return try JSONDecoder().decode([AzureSubscription].self, from: Data(json.utf8))
    }

    func isInstalled() -> Bool {
        ["/opt/homebrew/bin/az", "/usr/local/bin/az", "/usr/bin/az"].contains { path in
            FileManager.default.fileExists(atPath: path)
        }
    }

    private func resolveAzPath() -> String {
        for path in ["/opt/homebrew/bin/az", "/usr/local/bin/az", "/usr/bin/az"] {
            if FileManager.default.fileExists(atPath: path) { return path }
        }
        return "az"
    }
}
