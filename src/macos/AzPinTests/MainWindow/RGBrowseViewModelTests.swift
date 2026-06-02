import XCTest
@testable import AzPin

@MainActor
final class RGBrowseViewModelTests: XCTestCase {
    private var mockARM: MockARMService!
    private var vm: RGBrowseViewModel!

    override func setUp() {
        mockARM = MockARMService()
        vm = RGBrowseViewModel(arm: mockARM)
    }

    override func tearDown() {
        vm = nil
        mockARM = nil
    }

    func testLoadResources_populatesSortedByType() async {
        mockARM.resourcesResult = .success([
            AzureResource(id: "/r1", name: "my-app", type: "Microsoft.Web/sites", location: "eastus"),
            AzureResource(id: "/r2", name: "my-db", type: "Microsoft.Sql/servers", location: "eastus"),
            AzureResource(id: "/r3", name: "my-fn", type: "Microsoft.Web/sites/functions", location: "eastus")
        ])

        await vm.loadResources(subscriptionId: "sub-1", rgName: "rg-prod", tenantId: "")

        XCTAssertEqual(vm.resources.count, 3)
        XCTAssertEqual(vm.resources[0].type.lowercased(), "microsoft.sql/servers")
        XCTAssertEqual(vm.resources[1].type.lowercased(), "microsoft.web/sites")
        XCTAssertEqual(vm.resources[2].type.lowercased(), "microsoft.web/sites/functions")
        XCTAssertNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoading)
    }

    func testLoadResources_onError_setsErrorMessage() async {
        mockARM.resourcesResult = .failure(URLError(.badServerResponse))

        await vm.loadResources(subscriptionId: "sub-1", rgName: "rg-prod", tenantId: "")

        XCTAssertTrue(vm.resources.isEmpty)
        XCTAssertNotNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoading)
    }

    func testLoadResources_retryAfterError_clearsErrorAndLoads() async {
        mockARM.resourcesResult = .failure(URLError(.badServerResponse))
        await vm.loadResources(subscriptionId: "sub-1", rgName: "rg-prod", tenantId: "")
        XCTAssertNotNil(vm.errorMessage)

        mockARM.resourcesResult = .success([
            AzureResource(id: "/r1", name: "my-app", type: "Microsoft.Web/sites", location: "eastus")
        ])
        await vm.loadResources(subscriptionId: "sub-1", rgName: "rg-prod", tenantId: "")

        XCTAssertNil(vm.errorMessage)
        XCTAssertEqual(vm.resources.count, 1)
    }

    func testLoadResources_emptyResult_clearsResourcesAndNoError() async {
        mockARM.resourcesResult = .success([])

        await vm.loadResources(subscriptionId: "sub-1", rgName: "rg-empty", tenantId: "")

        XCTAssertTrue(vm.resources.isEmpty)
        XCTAssertNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoading)
    }
}
