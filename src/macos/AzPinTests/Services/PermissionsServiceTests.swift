import XCTest
@testable import AzPin

final class PermissionsServiceTests: XCTestCase {
    private var mockCache: MockTokenCache!
    private var session: URLSession!
    private var service: PermissionsService!

    private let resource = PinnedResource(
        id: "/subscriptions/sub-1/resourceGroups/rg-1/providers/Microsoft.Web/sites/my-app",
        name: "my-app",
        type: "microsoft.web/sites",
        resourceGroup: "rg-1",
        subscriptionId: "sub-1",
        location: "eastus",
        displayOrder: 0
    )

    override func setUp() {
        mockCache = MockTokenCache()
        session = MockURLProtocol.makeSession()
        service = PermissionsService(tokenCache: mockCache, session: session)
    }

    override func tearDown() {
        MockURLProtocol.handler = nil
        service = nil
        session = nil
        mockCache = nil
    }

    func test_canManage_returnsFalse_whenTokenFetchFails() async {
        mockCache.tokenResult = .failure(URLError(.badServerResponse))

        let result = await service.canManage(resource: resource)

        XCTAssertFalse(result)
    }

    func test_canManage_returnsFalse_whenResponseIsEmpty() async {
        mockCache.tokenResult = .success("token")
        MockURLProtocol.handler = { _ in
            let body = #"{"value": []}"#
            let response = HTTPURLResponse(url: URL(string: "https://example.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!
            return (response, Data(body.utf8))
        }

        let result = await service.canManage(resource: resource)

        XCTAssertFalse(result)
    }

    func test_canManage_returnsTrue_whenActionsPresent() async {
        mockCache.tokenResult = .success("token")
        MockURLProtocol.handler = { _ in
            let body = #"{"value": [{"actions": ["Microsoft.Web/sites/start/action", "Microsoft.Web/sites/stop/action"], "notActions": []}]}"#
            let response = HTTPURLResponse(url: URL(string: "https://example.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!
            return (response, Data(body.utf8))
        }

        let result = await service.canManage(resource: resource)

        XCTAssertTrue(result)
    }

    func test_canManage_returnsTrue_whenWildcardActionGranted() async {
        mockCache.tokenResult = .success("token")
        MockURLProtocol.handler = { _ in
            let body = #"{"value": [{"actions": ["*"], "notActions": []}]}"#
            let response = HTTPURLResponse(url: URL(string: "https://example.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!
            return (response, Data(body.utf8))
        }

        let result = await service.canManage(resource: resource)

        XCTAssertTrue(result)
    }

    func test_canManage_returnsFalse_whenActionGrantedButDeniedViaNotActions() async {
        mockCache.tokenResult = .success("token")
        MockURLProtocol.handler = { _ in
            let body = #"{"value": [{"actions": ["*"], "notActions": ["Microsoft.Web/sites/start/action"]}]}"#
            let response = HTTPURLResponse(url: URL(string: "https://example.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!
            return (response, Data(body.utf8))
        }

        let result = await service.canManage(resource: resource)

        XCTAssertFalse(result)
    }

    func test_permissionsAreCached_withinSameSession() async {
        mockCache.tokenResult = .success("token")
        var callCount = 0
        MockURLProtocol.handler = { _ in
            callCount += 1
            let body = #"{"value": [{"actions": ["*"], "notActions": []}]}"#
            let response = HTTPURLResponse(url: URL(string: "https://example.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!
            return (response, Data(body.utf8))
        }

        _ = await service.canManage(resource: resource)
        _ = await service.canManage(resource: resource)

        XCTAssertEqual(callCount, 1)
    }

    func test_canManage_returnsFalse_whenResponseMalformed() async {
        mockCache.tokenResult = .success("token")
        MockURLProtocol.handler = { _ in
            let response = HTTPURLResponse(url: URL(string: "https://example.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!
            return (response, Data("not json".utf8))
        }

        let result = await service.canManage(resource: resource)

        XCTAssertFalse(result)
    }
}
