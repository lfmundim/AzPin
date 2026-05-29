import SwiftData

@Model
final class PinnedResource {
    var id: String
    var name: String
    var type: String
    var resourceGroup: String
    var subscriptionId: String
    var location: String
    var displayOrder: Int

    init(id: String, name: String, type: String, resourceGroup: String, subscriptionId: String, location: String, displayOrder: Int) {
        self.id = id
        self.name = name
        self.type = type
        self.resourceGroup = resourceGroup
        self.subscriptionId = subscriptionId
        self.location = location
        self.displayOrder = displayOrder
    }
}
