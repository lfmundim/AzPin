import XCTest
@testable import AzPin

final class ARMServiceTests: XCTestCase {

    // MARK: - apiVersion

    func testApiVersion_appService_returns2023_01_01() {
        XCTAssertEqual(ARMService.apiVersion(for: "Microsoft.Web/sites"), "2023-01-01")
    }

    func testApiVersion_containerApp_returns2023_05_01() {
        XCTAssertEqual(ARMService.apiVersion(for: "Microsoft.App/containerApps"), "2023-05-01")
    }

    func testApiVersion_logicApp_returns2019_05_01() {
        XCTAssertEqual(ARMService.apiVersion(for: "Microsoft.Logic/workflows"), "2019-05-01")
    }

    func testApiVersion_caseInsensitive() {
        XCTAssertEqual(ARMService.apiVersion(for: "MICROSOFT.APP/CONTAINERAPPS"), "2023-05-01")
    }

    func testApiVersion_unknown_returnsDefault() {
        XCTAssertEqual(ARMService.apiVersion(for: "Microsoft.Unknown/type"), "2023-01-01")
    }

    // MARK: - mapAction

    func testMapAction_logicApp_startMapsToEnable() {
        XCTAssertEqual(ARMService.mapAction("start", for: "Microsoft.Logic/workflows"), "enable")
    }

    func testMapAction_logicApp_stopMapsToDisable() {
        XCTAssertEqual(ARMService.mapAction("stop", for: "Microsoft.Logic/workflows"), "disable")
    }

    func testMapAction_logicApp_restartPassesThrough() {
        XCTAssertEqual(ARMService.mapAction("restart", for: "Microsoft.Logic/workflows"), "restart")
    }

    func testMapAction_appService_passesThrough() {
        XCTAssertEqual(ARMService.mapAction("start", for: "Microsoft.Web/sites"), "start")
        XCTAssertEqual(ARMService.mapAction("stop", for: "Microsoft.Web/sites"), "stop")
        XCTAssertEqual(ARMService.mapAction("restart", for: "Microsoft.Web/sites"), "restart")
    }

    // MARK: - parseAppState

    func testParseAppState_appService_running() throws {
        let json = #"{"properties":{"state":"Running"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "Microsoft.Web/sites")
        XCTAssertEqual(state, .running)
    }

    func testParseAppState_appService_stopped() throws {
        let json = #"{"properties":{"state":"Stopped"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "Microsoft.Web/sites")
        XCTAssertEqual(state, .stopped)
    }

    func testParseAppState_containerApp_running() throws {
        let json = #"{"properties":{"runningStatus":"Running"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "Microsoft.App/containerApps")
        XCTAssertEqual(state, .running)
    }

    func testParseAppState_containerApp_stopped() throws {
        let json = #"{"properties":{"runningStatus":"Stopped"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "Microsoft.App/containerApps")
        XCTAssertEqual(state, .stopped)
    }

    func testParseAppState_logicApp_enabledMapsToRunning() throws {
        let json = #"{"properties":{"state":"Enabled"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "Microsoft.Logic/workflows")
        XCTAssertEqual(state, .running)
    }

    func testParseAppState_logicApp_disabledMapsToStopped() throws {
        let json = #"{"properties":{"state":"Disabled"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "Microsoft.Logic/workflows")
        XCTAssertEqual(state, .stopped)
    }

    func testParseAppState_caseInsensitive() throws {
        let json = #"{"properties":{"runningStatus":"RUNNING"}}"#.data(using: .utf8)!
        let state = try ARMService.parseAppState(from: json, resourceType: "microsoft.app/containerapps")
        XCTAssertEqual(state, .running)
    }
}
