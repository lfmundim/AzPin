//
//  AuthViewModel.swift
//  AzPin
//
//  Created by Lucas Mundim on 31/05/2026.
//
import Foundation

enum AuthState {
    case unknown
    case cliNotInstalled
    case notSignedIn
    case signedIn(account: AzureAccount)
    case error(String)
}

@MainActor
@Observable
final class AuthViewModel {
    private let azCLI: any AzCLIServiceProtocol
    var state: AuthState = .unknown

    init(azCLI: any AzCLIServiceProtocol) {
        self.azCLI = azCLI
    }

    func refresh() async {
        guard azCLI.isInstalled() else {
            state = .cliNotInstalled
            return
        }
        do {
            let account = try await azCLI.currentAccount()
            state = .signedIn(account: account)
        } catch {
            state = .notSignedIn
        }
    }
}
