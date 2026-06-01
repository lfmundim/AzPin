import SwiftData

@Model
final class PinnedResourceGroup {
    var id: String
    var subscriptionId: String
    var subscriptionDisplayName: String?
    var name: String
    var displayOrder: Int
    @Relationship(deleteRule: .cascade) var resources: [PinnedResource]

    init(id: String, subscriptionId: String, name: String, displayOrder: Int, subscriptionDisplayName: String? = nil) {
        self.id = id
        self.subscriptionId = subscriptionId
        self.subscriptionDisplayName = subscriptionDisplayName
        self.name = name
        self.displayOrder = displayOrder
        self.resources = []
    }
}
