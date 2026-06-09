import XCTest
@testable import AzPin

final class UpdateCheckServiceTests: XCTestCase {

    // MARK: - isNewer

    func testIsNewer_laterMinor_returnsTrue() {
        XCTAssertTrue(UpdateCheckService.isNewer("1.1.0", than: "1.0.0"))
    }

    func testIsNewer_laterMajor_returnsTrue() {
        XCTAssertTrue(UpdateCheckService.isNewer("2.0.0", than: "1.9.9"))
    }

    func testIsNewer_laterPatch_returnsTrue() {
        XCTAssertTrue(UpdateCheckService.isNewer("1.0.1", than: "1.0.0"))
    }

    func testIsNewer_sameVersion_returnsFalse() {
        XCTAssertFalse(UpdateCheckService.isNewer("1.0.0", than: "1.0.0"))
    }

    func testIsNewer_olderVersion_returnsFalse() {
        XCTAssertFalse(UpdateCheckService.isNewer("0.9.9", than: "1.0.0"))
    }

    func testIsNewer_missingPatchComponent_treatedAsZero() {
        XCTAssertFalse(UpdateCheckService.isNewer("1.0", than: "1.0.0"))
        XCTAssertTrue(UpdateCheckService.isNewer("1.1", than: "1.0.0"))
    }

    // MARK: - checkForUpdates (network)

    func testCheckForUpdates_updateAvailable() async throws {
        let session = makeSession { _ in
            let json = #"{"tag_name":"v2.0.0","html_url":"https://github.com/lfmundim/AzPin/releases/tag/v2.0.0"}"#
            return (HTTPURLResponse(url: URL(string: "https://api.github.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!, Data(json.utf8))
        }
        let service = UpdateCheckService(session: session)
        await service.checkForUpdates()

        if case .updateAvailable(_, let latest, _) = service.state {
            XCTAssertEqual(latest, "2.0.0")
        } else {
            XCTFail("Expected updateAvailable, got \(service.state)")
        }
    }

    func testCheckForUpdates_upToDate() async throws {
        let current = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "0.0.0"
        let session = makeSession { _ in
            let json = #"{"tag_name":"v\#(current)","html_url":"https://github.com/lfmundim/AzPin/releases/tag/v\#(current)"}"#
            return (HTTPURLResponse(url: URL(string: "https://api.github.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!, Data(json.utf8))
        }
        let service = UpdateCheckService(session: session)
        await service.checkForUpdates()

        if case .upToDate = service.state { } else {
            XCTFail("Expected upToDate, got \(service.state)")
        }
    }

    func testCheckForUpdates_networkError_setsFailedState() async {
        MockURLProtocol.handler = nil
        let config = URLSessionConfiguration.ephemeral
        config.protocolClasses = [MockURLProtocol.self]
        let session = URLSession(configuration: config)
        let service = UpdateCheckService(session: session)
        await service.checkForUpdates()

        if case .failed = service.state { } else {
            XCTFail("Expected failed, got \(service.state)")
        }
    }

    func testCheckForUpdates_invalidJSON_setsFailedState() async {
        let session = makeSession { _ in
            return (HTTPURLResponse(url: URL(string: "https://api.github.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!, Data("not json".utf8))
        }
        let service = UpdateCheckService(session: session)
        await service.checkForUpdates()

        if case .failed = service.state { } else {
            XCTFail("Expected failed, got \(service.state)")
        }
    }

    func testCheckForUpdates_setsUserAgentHeader() async {
        var capturedRequest: URLRequest?
        let session = makeSession { req in
            capturedRequest = req
            let json = #"{"tag_name":"v0.0.1","html_url":"https://github.com"}"#
            return (HTTPURLResponse(url: URL(string: "https://api.github.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!, Data(json.utf8))
        }
        let service = UpdateCheckService(session: session)
        await service.checkForUpdates()
        XCTAssertEqual(capturedRequest?.value(forHTTPHeaderField: "User-Agent"), "AzPin")
    }

    func testCheckForUpdates_stripsPrefixV() async {
        let session = makeSession { _ in
            let json = #"{"tag_name":"v99.0.0","html_url":"https://github.com"}"#
            return (HTTPURLResponse(url: URL(string: "https://api.github.com")!, statusCode: 200, httpVersion: nil, headerFields: nil)!, Data(json.utf8))
        }
        let service = UpdateCheckService(session: session)
        await service.checkForUpdates()

        if case .updateAvailable(_, let latest, _) = service.state {
            XCTAssertEqual(latest, "99.0.0")
        } else {
            XCTFail("Expected updateAvailable, got \(service.state)")
        }
    }

    // MARK: - helpers

    private func makeSession(handler: @escaping (URLRequest) -> (HTTPURLResponse, Data)) -> URLSession {
        MockURLProtocol.handler = handler
        return MockURLProtocol.makeSession()
    }
}
