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
}
