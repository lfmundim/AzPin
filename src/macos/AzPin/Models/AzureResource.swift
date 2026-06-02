import Foundation

struct AzureResource: Decodable, Sendable {
    let id: String
    let name: String
    let type: String
    let location: String
    let tags: [String: String]?

    init(id: String, name: String, type: String, location: String, tags: [String: String]? = nil) {
        self.id = id
        self.name = name
        self.type = type
        self.location = location
        self.tags = tags
    }
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
        case expiresOn = "expires_on"
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

struct AzureSubscription: Decodable, Sendable, Hashable {
    let id: String
    let name: String
    let tenantId: String
    let isDefault: Bool

    init(id: String, name: String, tenantId: String, isDefault: Bool = false) {
        self.id = id
        self.name = name
        self.tenantId = tenantId
        self.isDefault = isDefault
    }

    init(from decoder: Decoder) throws {
        let c = try decoder.container(keyedBy: CodingKeys.self)
        id = try c.decode(String.self, forKey: .id)
        name = try c.decode(String.self, forKey: .name)
        tenantId = try c.decode(String.self, forKey: .tenantId)
        isDefault = try c.decodeIfPresent(Bool.self, forKey: .isDefault) ?? false
    }

    private enum CodingKeys: String, CodingKey {
        case id, name, tenantId, isDefault
    }
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

struct ContainerAppResponse: Decodable, Sendable {
    let properties: ContainerAppProperties
}

struct ContainerAppProperties: Decodable, Sendable {
    let runningStatus: String
}
