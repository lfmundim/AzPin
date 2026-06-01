import SwiftData

@Model
final class PinnedResourceGroup {
    var id: String
    var subscriptionId: String
    var name: String
    var displayOrder: Int
    @Relationship(deleteRule: .cascade) var resources: [PinnedResource]

    init(id: String, subscriptionId: String, name: String, displayOrder: Int) {
        self.id = id
        self.subscriptionId = subscriptionId
        self.name = name
        self.displayOrder = displayOrder
        self.resources = []
    }
}
