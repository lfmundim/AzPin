import Foundation

struct AzureResource: Decodable, Sendable {
    let id: String
    let name: String
    let type: String
    let location: String
}

struct AzureResourceGroup: Decodable, Sendable {
    let id: String
    let name: String
    let location: String
}

struct AzureTokenResponse: Decodable, Sendable {
    let accessToken: String
    let expiresOn: Date

    enum CodingKeys: String, CodingKey {
        case accessToken = "accessToken"
        case expiresOn = "expiresOn"
    }
}

struct AzureAccount: Decodable, Sendable {
    let id: String
    let name: String
    let tenantId: String
    let user: AzureAccountUser
}

struct AzureAccountUser: Decodable, Sendable {
    let name: String
}

struct AzureSubscription: Decodable, Sendable {
    let id: String
    let name: String
    let tenantId: String
}

struct ResourceListResponse: Decodable, Sendable {
    let value: [AzureResource]
}

struct ResourceGroupListResponse: Decodable, Sendable {
    let value: [AzureResourceGroup]
}

struct AppServiceResponse: Decodable, Sendable {
    let properties: AppServiceProperties
}

struct AppServiceProperties: Decodable, Sendable {
    let state: String
}
