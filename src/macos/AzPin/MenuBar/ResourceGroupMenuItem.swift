import SwiftUI

struct ResourceGroupMenuItem: View {
    let name: String

    var body: some View {
        Label(name, systemImage: "folder.fill")
    }
}
