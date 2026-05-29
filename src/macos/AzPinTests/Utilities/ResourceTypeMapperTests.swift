import XCTest
@testable import AzPin

final class ResourceTypeMapperTests: XCTestCase {
    func test_functionApp_resolvesBoltFill() {
        XCTAssertEqual(ResourceTypeMapper.symbolName(for: "microsoft.web/serverfarms"), "bolt.fill")
    }

    func test_appService_resolvesGlobe() {
        XCTAssertEqual(ResourceTypeMapper.symbolName(for: "Microsoft.Web/sites"), "globe")
    }

    func test_appService_caseInsensitive() {
        XCTAssertEqual(ResourceTypeMapper.symbolName(for: "MICROSOFT.WEB/SITES"), "globe")
    }

    func test_appInsights_resolvesLightbulbFill() {
        XCTAssertEqual(ResourceTypeMapper.symbolName(for: "microsoft.insights/components"), "lightbulb.fill")
    }

    func test_unknownType_resolvesCloudFill() {
        XCTAssertEqual(ResourceTypeMapper.symbolName(for: "microsoft.unknown/thing"), "cloud.fill")
    }

    func test_runnableTypes_allReturnTrue() {
        let runnable = [
            "microsoft.web/sites",
            "microsoft.web/sites/slots",
            "microsoft.app/containerapps",
            "microsoft.logic/workflows"
        ]
        for type in runnable {
            XCTAssertTrue(ResourceTypeMapper.isRunnable(type), "\(type) should be runnable")
        }
    }

    func test_runnableTypes_caseInsensitive() {
        XCTAssertTrue(ResourceTypeMapper.isRunnable("Microsoft.Web/sites"))
        XCTAssertTrue(ResourceTypeMapper.isRunnable("MICROSOFT.WEB/SITES"))
    }

    func test_appInsights_isNotRunnable() {
        XCTAssertFalse(ResourceTypeMapper.isRunnable("microsoft.insights/components"))
    }

    func test_storageAccount_isNotRunnable() {
        XCTAssertFalse(ResourceTypeMapper.isRunnable("microsoft.storage/storageaccounts"))
    }
}
