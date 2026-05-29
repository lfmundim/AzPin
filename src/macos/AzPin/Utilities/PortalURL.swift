import Foundation

enum PortalURL {
    static func resource(id: String) -> URL {
        let encoded = id.addingPercentEncoding(withAllowedCharacters: .urlPathAllowed) ?? id
        return URL(string: "https://portal.azure.com/#resource\(encoded)")!
    }

    static func resourceGroup(subscriptionId: String, name: String) -> URL {
        let path = "/subscriptions/\(subscriptionId)/resourceGroups/\(name)"
        return URL(string: "https://portal.azure.com/#resource\(path)")!
    }
}
