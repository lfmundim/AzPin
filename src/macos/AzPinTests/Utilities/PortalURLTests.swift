import XCTest
@testable import AzPin

final class PortalURLTests: XCTestCase {
    func test_resourceURL_constructedCorrectly() {
        let id = "/subscriptions/sub-1/resourceGroups/rg-1/providers/Microsoft.Web/sites/my-app"
        let url = PortalURL.resource(id: id)
        XCTAssertEqual(url.absoluteString, "https://portal.azure.com/#resource\(id)")
    }

    func test_resourceGroupURL_constructedCorrectly() {
        let url = PortalURL.resourceGroup(subscriptionId: "sub-1", name: "rg-1")
        XCTAssertEqual(url.absoluteString, "https://portal.azure.com/#resource/subscriptions/sub-1/resourceGroups/rg-1")
    }

    func test_resourceId_notDoublePrefixed() {
        let id = "/subscriptions/sub-1/resourceGroups/rg-1/providers/Microsoft.Web/sites/app"
        let url = PortalURL.resource(id: id)
        XCTAssertFalse(url.absoluteString.contains("/#resource/subscriptions/sub-1/resourceGroups/rg-1/providers/Microsoft.Web/sites/app/subscriptions"))
    }
}
