import SwiftData
import Foundation

@Model
final class CachedToken {
    var subscriptionId: String
    var tenantId: String
    var accessToken: String
    var expiresOn: Date

    init(subscriptionId: String, tenantId: String, accessToken: String, expiresOn: Date) {
        self.subscriptionId = subscriptionId
        self.tenantId = tenantId
        self.accessToken = accessToken
        self.expiresOn = expiresOn
    }
}
