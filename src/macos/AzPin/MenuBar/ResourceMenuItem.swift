import SwiftUI

struct ResourceMenuItem: View {
    let name: String
    let symbolName: String

    var body: some View {
        Label(name, systemImage: symbolName)
    }
}
