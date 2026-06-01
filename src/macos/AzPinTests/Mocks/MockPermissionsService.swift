import Foundation
@testable import AzPin

final class MockPermissionsService: PermissionsServiceProtocol {
    let canManageResult = true

    func canManage(resource: PinnedResource) async -> Bool {
        canManageResult
    }
}
