import Foundation

protocol AzCLIServiceProtocol: Sendable {
    func fetchToken(subscriptionId: String, tenantId: String) async throws -> AzureTokenResponse
    func currentAccount() async throws -> AzureAccount
    func listSubscriptions() async throws -> [AzureSubscription]
    func isInstalled() -> Bool
}

extension AzCLIServiceProtocol {
    func fetchToken(subscriptionId: String) async throws -> AzureTokenResponse {
        try await fetchToken(subscriptionId: subscriptionId, tenantId: "")
    }
}

@Observable
final class AzCLIService: AzCLIServiceProtocol {
    private let shell: ShellRunner
    private let jsonDecoder: AzJSONDecoder

    init(shell: ShellRunner = ShellRunner(), jsonDecoder: AzJSONDecoder = AzJSONDecoder()) {
        self.shell = shell
        self.jsonDecoder = jsonDecoder
    }

    func fetchToken(subscriptionId: String, tenantId: String) async throws -> AzureTokenResponse {
        let json = try await shell.run("\(resolveAzPath()) account get-access-token --subscription \(subscriptionId) --output json")
        return try jsonDecoder.decode(AzureTokenResponse.self, from: json)
    }

    func currentAccount() async throws -> AzureAccount {
        let json = try await shell.run("\(resolveAzPath()) account show --output json")
        return try jsonDecoder.decode(AzureAccount.self, from: json)
    }

    func listSubscriptions() async throws -> [AzureSubscription] {
        let json = try await shell.run("\(resolveAzPath()) account list --output json")
        return try jsonDecoder.decode([AzureSubscription].self, from: json)
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
