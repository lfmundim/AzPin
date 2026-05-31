import XCTest
@testable import AzPin

@MainActor
final class BrowseViewModelTests: XCTestCase {
    private var mockAzCLI: MockAzCLIService!
    private var mockARM: MockARMService!
    private var vm: BrowseViewModel!

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
        let subs = [
            AzureSubscription(id: "sub-1", name: "Prod", tenantId: "tenant"),
            AzureSubscription(id: "sub-2", name: "Dev", tenantId: "tenant")
        ]
        mockAzCLI.subscriptionsResult = .success(subs)

        await vm.loadSubscriptions()

        XCTAssertEqual(vm.subscriptions.count, 2)
        XCTAssertEqual(vm.selectedSubscriptionId, "sub-1")
        XCTAssertNil(vm.errorMessage)
        XCTAssertFalse(vm.isLoadingSubscriptions)
    }

    func testLoadSubscriptions_preservesExistingSelection() async {
        let subs = [
            AzureSubscription(id: "sub-1", name: "Prod", tenantId: "tenant"),
            AzureSubscription(id: "sub-2", name: "Dev", tenantId: "tenant")
        ]
        mockAzCLI.subscriptionsResult = .success(subs)
        vm.selectedSubscriptionId = "sub-2"

        await vm.loadSubscriptions()

        XCTAssertEqual(vm.selectedSubscriptionId, "sub-2")
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

        let subs = [AzureSubscription(id: "sub-1", name: "Prod", tenantId: "tenant")]
        mockAzCLI.subscriptionsResult = .success(subs)
        await vm.loadSubscriptions()

        XCTAssertNil(vm.errorMessage)
        XCTAssertEqual(vm.subscriptions.count, 1)
    }

    func testLoadSubscriptions_triggersResourceGroupLoad() async {
        let subs = [AzureSubscription(id: "sub-1", name: "Prod", tenantId: "tenant")]
        let rgs = [AzureResourceGroup(id: "/rg-1", name: "rg-prod", location: "eastus")]
        mockAzCLI.subscriptionsResult = .success(subs)
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
        vm.selectedSubscriptionId = "sub-1"
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
        vm.selectedSubscriptionId = "sub-1"
        mockARM.resourceGroupsResult = .failure(URLError(.badServerResponse))

        await vm.loadResourceGroups()

        XCTAssertNotNil(vm.errorMessage)
        XCTAssertTrue(vm.resourceGroups.isEmpty)
        XCTAssertFalse(vm.isLoadingResourceGroups)
    }
}
