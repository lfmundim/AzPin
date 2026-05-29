import Foundation
@testable import AzPin

final class MockPermissionsService: PermissionsServiceProtocol {
    var canManageResult = true

    func canManage(resource: PinnedResource) async -> Bool {
        canManageResult
    }
}
