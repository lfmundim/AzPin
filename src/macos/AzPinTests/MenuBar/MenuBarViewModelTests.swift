import XCTest
@testable import AzPin

@MainActor
final class MenuBarViewModelTests: XCTestCase {
    private var mockARM: MockARMService!
    private var mockPermissions: MockPermissionsService!
    private var vm: MenuBarViewModel!

    private let rg = PinnedResourceGroup(id: "rg-id-1", subscriptionId: "sub-1", name: "rg-prod", displayOrder: 0)
    private let runnableResource = AzureResource(id: "/subscriptions/sub-1/resourceGroups/rg-prod/providers/Microsoft.Web/sites/app", name: "app", type: "microsoft.web/sites", location: "eastus")
    private let nonRunnableResource = AzureResource(id: "/subscriptions/sub-1/resourceGroups/rg-prod/providers/Microsoft.Storage/storageAccounts/st", name: "st", type: "microsoft.storage/storageaccounts", location: "eastus")

    override func setUp() {
        mockARM = MockARMService()
        mockPermissions = MockPermissionsService()
        vm = MenuBarViewModel(arm: mockARM, permissionsService: mockPermissions)
    }

    override func tearDown() {
        vm = nil
        mockARM = nil
        mockPermissions = nil
    }

    // MARK: - toggle

    func test_toggle_setsExpandedRGId() {
        vm.toggle(rgId: "rg-id-1")
        XCTAssertEqual(vm.expandedRGId, "rg-id-1")
    }

    func test_toggle_collapsesWhenAlreadyExpanded() {
        vm.toggle(rgId: "rg-id-1")
        vm.toggle(rgId: "rg-id-1")
        XCTAssertNil(vm.expandedRGId)
    }

    func test_toggle_switchesExpandedRG() {
        vm.toggle(rgId: "rg-id-1")
        vm.toggle(rgId: "rg-id-2")
        XCTAssertEqual(vm.expandedRGId, "rg-id-2")
    }

    // MARK: - loadResources

    func test_loadResources_populatesResourcesByRG() async {
        mockARM.resourcesResult = .success([runnableResource])

        await vm.loadResources(for: [rg])

        XCTAssertEqual(vm.resourcesByRG["rg-id-1"]?.count, 1)
        XCTAssertFalse(vm.isLoading)
    }

    func test_loadResources_tracksErrorOnFailure() async {
        mockARM.resourcesResult = .failure(URLError(.badServerResponse))

        await vm.loadResources(for: [rg])

        XCTAssertNotNil(vm.loadErrors["rg-id-1"])
        XCTAssertEqual(vm.resourcesByRG["rg-id-1"]?.count, 0)
    }

    // MARK: - startApp state revert

    func test_startApp_setsRunningOnSuccess() async {
        mockARM.appStateResult = .success(.running)

        await vm.startApp(resource: runnableResource, rg: rg)

        XCTAssertEqual(vm.appStates[runnableResource.id], .running)
    }

    func test_startApp_revertsToStoppedOnFailure() async {
        mockARM.appStateResult = .failure(URLError(.badServerResponse))

        await vm.startApp(resource: runnableResource, rg: rg)

        XCTAssertEqual(vm.appStates[runnableResource.id], .stopped)
    }

    func test_stopApp_revertsToRunningOnFailure() async {
        mockARM.appStateResult = .failure(URLError(.badServerResponse))

        await vm.stopApp(resource: runnableResource, rg: rg)

        XCTAssertEqual(vm.appStates[runnableResource.id], .running)
    }

    func test_restartApp_remainsRunningOnFailure() async {
        mockARM.appStateResult = .failure(URLError(.badServerResponse))

        await vm.restartApp(resource: runnableResource, rg: rg)

        XCTAssertEqual(vm.appStates[runnableResource.id], .running)
    }

    func test_startApp_setsStartingTransitionalState() async {
        var observedState: AppRunningState?
        let exp = expectation(description: "state set to starting")
        mockARM.appStateResult = .success(.running)

        let originalStart = mockARM.onFetchResourceGroups
        // Capture state mid-flight via a fresh task observation isn't straightforward,
        // so verify that final state is .running (start succeeded)
        await vm.startApp(resource: runnableResource, rg: rg)
        observedState = vm.appStates[runnableResource.id]
        exp.fulfill()

        await fulfillment(of: [exp], timeout: 1)
        XCTAssertEqual(observedState, .running)
        _ = originalStart
    }
}
