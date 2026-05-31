import SwiftUI

struct ResourceGroupRow: View {
    let resourceGroup: AzureResourceGroup

    var body: some View {
        Label(resourceGroup.name, systemImage: "folder.fill")
    }
}
