import XCTest
@testable import AzPin

@MainActor
final class BrowseViewModelTests: XCTestCase {
    private var mockAzCLI: MockAzCLIService!
    private var mockARM: MockARMService!
    private var vm: BrowseViewModel!

    private let sub1 = AzureSubscription(id: "sub-1", name: "Prod", tenantId: "tenant-a")
    private let sub2 = AzureSubscription(id: "sub-2", name: "Dev", tenantId: "tenant-b")

    override func setUp() {
        mockAzCLI = MockAzCLIService()
        mockARM = MockARMService()
        vm = BrowseViewModel(azCLI: mockAzCLI, arm: mockARM)
    }

    override func tearDown() {
        vm = nil
        mockAzCLI = nil
        mockARM = nil
    }

    // MARK: - loadSubscriptions

    func testLoadSubscriptions_populatesListAndAutoSelectsFirst() async {
        mockAzCLI.subscriptionsResult = .success([sub1, sub2])

        await vm.loadSubscriptions()

        XCTAssertEqual(vm.subscriptions.count, 2)
        XCTAssertEqual(vm.selectedSubscription?.id, "sub-1")
        XCTAssertNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoadingSubscriptions)
    }

    func testLoadSubscriptions_preservesExistingSelection() async {
        mockAzCLI.subscriptionsResult = .success([sub1, sub2])
        vm.selectedSubscription = sub2

        await vm.loadSubscriptions()

        XCTAssertEqual(vm.selectedSubscription?.id, "sub-2")
    }

    func testLoadSubscriptions_onError_setsErrorMessageAndClearsLoading() async {
        mockAzCLI.subscriptionsResult = .failure(URLError(.badServerResponse))

        await vm.loadSubscriptions()

        XCTAssertNotNil(vm.errorMessage)
        XCTAssertTrue(vm.subscriptions.isEmpty)
        XCTAssertFalse(vm.isLoadingSubscriptions)
    }

    func testLoadSubscriptions_retryAfterError_clearsErrorAndLoadsData() async {
        mockAzCLI.subscriptionsResult = .failure(URLError(.badServerResponse))
        await vm.loadSubscriptions()
        XCTAssertNotNil(vm.errorMessage)

        mockAzCLI.subscriptionsResult = .success([sub1])
        await vm.loadSubscriptions()

        XCTAssertNil(vm.errorMessage)
        XCTAssertEqual(vm.subscriptions.count, 1)
    }

    func testLoadSubscriptions_triggersResourceGroupLoad() async {
        let rgs = [AzureResourceGroup(id: "/rg-1", name: "rg-prod", location: "eastus")]
        mockAzCLI.subscriptionsResult = .success([sub1])
        mockARM.resourceGroupsResult = .success(rgs)

        await vm.loadSubscriptions()

        XCTAssertEqual(vm.resourceGroups.count, 1)
        XCTAssertEqual(vm.resourceGroups.first?.name, "rg-prod")
    }

    // MARK: - loadResourceGroups

    func testLoadResourceGroups_noopWhenNoSubscriptionSelected() async {
        await vm.loadResourceGroups()

        XCTAssertTrue(vm.resourceGroups.isEmpty)
        XCTAssertFalse(vm.isLoadingResourceGroups)
    }

    func testLoadResourceGroups_populatesList() async {
        vm.selectedSubscription = sub1
        let rgs = [
            AzureResourceGroup(id: "/rg-1", name: "rg-prod", location: "eastus"),
            AzureResourceGroup(id: "/rg-2", name: "rg-dev", location: "westus")
        ]
        mockARM.resourceGroupsResult = .success(rgs)

        await vm.loadResourceGroups()

        XCTAssertEqual(vm.resourceGroups.count, 2)
        XCTAssertNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoadingResourceGroups)
    }

    func testLoadResourceGroups_onError_setsErrorMessage() async {
        vm.selectedSubscription = sub1
        mockARM.resourceGroupsResult = .failure(URLError(.badServerResponse))

        await vm.loadResourceGroups()

        XCTAssertNotNil(vm.errorMessage)
        XCTAssertTrue(vm.resourceGroups.isEmpty)
        XCTAssertFalse(vm.isLoadingResourceGroups)
    }

    func testLoadResourceGroups_resetsResourceState() async {
        vm.selectedSubscription = sub1
        vm.selectedResourceGroupName = "old-rg"
        vm.resources = [AzureResource(id: "/old", name: "old", type: "Microsoft.Web/sites", location: "eastus")]
        mockARM.resourceGroupsResult = .success([])

        await vm.loadResourceGroups()

        XCTAssertNil(vm.selectedResourceGroupName)
        XCTAssertTrue(vm.resources.isEmpty)
    }

    // MARK: - loadResources

    func testLoadResources_noopWhenNoSubscriptionSelected() async {
        await vm.loadResources(in: "rg-prod")

        XCTAssertTrue(vm.resources.isEmpty)
        XCTAssertFalse(vm.isLoadingResources)
    }

    func testLoadResources_populatesList() async {
        vm.selectedSubscription = sub1
        let res = [
            AzureResource(id: "/r1", name: "my-app", type: "Microsoft.Web/sites", location: "eastus"),
            AzureResource(id: "/r2", name: "my-fn", type: "Microsoft.Web/sites/functions", location: "eastus")
        ]
        mockARM.resourcesResult = .success(res)

        await vm.loadResources(in: "rg-prod")

        XCTAssertEqual(vm.resources.count, 2)
        XCTAssertNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoadingResources)
    }

    func testLoadResources_onError_setsErrorMessage() async {
        vm.selectedSubscription = sub1
        mockARM.resourcesResult = .failure(URLError(.badServerResponse))

        await vm.loadResources(in: "rg-prod")

        XCTAssertNotNil(vm.errorMessage)
        XCTAssertTrue(vm.resources.isEmpty)
        XCTAssertFalse(vm.isLoadingResources)
    }
}
