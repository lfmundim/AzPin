import SwiftUI

struct ResourceRow: View {
    let resource: AzureResource

    var body: some View {
        Label(resource.name, systemImage: ResourceTypeMapper.symbolName(for: resource.type))
            .font(.body)
    }
}
