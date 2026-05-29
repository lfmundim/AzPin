# Skill: Testing Approach

Use this file when writing tests, setting up mocks, or deciding what to test. AzPin has no sandbox, shells out to `az`, and makes network calls — all of which must be abstracted behind protocols for testability.

---

## Philosophy

Test the service layer thoroughly. Test views minimally. Do not test SwiftUI layout — that is what Preview is for.

Priority order:
1. `ARMService` — core business logic, most complex
2. `TokenCache` — expiry and refresh logic
3. `PermissionsService` — fail-safe behavior
4. `ResourceTypeMapper` — pure function, trivial to test exhaustively
5. `PortalURL` — pure function
6. `ShellRunner` — integration test only, not unit tested

---

## Protocol-Based Mocking

Every service that makes a network call or shells out must conform to a protocol. Views and other services depend on the protocol, not the concrete type.

### ARMServiceProtocol

```swift
protocol ARMServiceProtocol {
    func fetchResourceGroups(subscriptionId: String) async throws -> [AzureResourceGroup]
    func fetchResources(subscriptionId: String, resourceGroup: String) async throws -> [AzureResource]
    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState
    func startApp(resource: PinnedResource) async throws
    func stopApp(resource: PinnedResource) async throws
    func restartApp(resource: PinnedResource) async throws
}
```

### TokenCacheProtocol

```swift
protocol TokenCacheProtocol {
    func token(for subscriptionId: String) async throws -> String
    func invalidate(subscriptionId: String)
}
```

### PermissionsServiceProtocol

```swift
protocol PermissionsServiceProtocol {
    func canManage(resource: PinnedResource) async -> Bool
}
```

### AzCLIServiceProtocol

```swift
protocol AzCLIServiceProtocol {
    func fetchToken(subscriptionId: String) async throws -> AzureTokenResponse
    func currentAccount() async throws -> AzureAccount
    func listSubscriptions() async throws -> [AzureSubscription]
    func isInstalled() -> Bool
}
```

---

## Mock Implementations

Keep mocks in `AzPinTests/Mocks/`. Each mock is a simple struct or class with injectable return values.

```swift
// AzPinTests/Mocks/MockARMService.swift

final class MockARMService: ARMServiceProtocol {
    var resourceGroupsResult: Result<[AzureResourceGroup], Error> = .success([])
    var resourcesResult: Result<[AzureResource], Error> = .success([])
    var appStateResult: Result<AppRunningState, Error> = .success(.running)
    var startAppCalled = false
    var stopAppCalled = false
    var restartAppCalled = false

    func fetchResourceGroups(subscriptionId: String) async throws -> [AzureResourceGroup] {
        try resourceGroupsResult.get()
    }

    func fetchResources(subscriptionId: String, resourceGroup: String) async throws -> [AzureResource] {
        try resourcesResult.get()
    }

    func fetchAppState(resource: PinnedResource) async throws -> AppRunningState {
        try appStateResult.get()
    }

    func startApp(resource: PinnedResource) async throws {
        startAppCalled = true
        if case .failure(let error) = appStateResult { throw error }
    }

    func stopApp(resource: PinnedResource) async throws {
        stopAppCalled = true
        if case .failure(let error) = appStateResult { throw error }
    }

    func restartApp(resource: PinnedResource) async throws {
        restartAppCalled = true
        if case .failure(let error) = appStateResult { throw error }
    }
}
```

---

## What to Test

### TokenCache

```swift
// Expiry logic
func test_tokenIsReused_whenNotExpired()
func test_tokenIsRefreshed_whenExpiredOrWithin5Minutes()
func test_tokenIsRefreshed_whenMissing()
func test_refreshFailure_throws()
```

### PermissionsService

```swift
// Fail-safe: if checkAccess throws, return false
func test_canManage_returnsFalse_whenCheckAccessFails()
func test_canManage_returnsFalse_whenActionNotInResponse()
func test_canManage_returnsTrue_whenAllActionsPresent()
// Caching
func test_permissionsAreCached_withinSameSession()
```

### ResourceTypeMapper

Exhaustive — test every known type string, including mixed casing:

```swift
func test_functionApp_resolvesBoltFill()
func test_functionApp_caseInsensitive()
func test_appService_resolvesGlobe()
func test_appInsights_resolvesLightbulbFill()
func test_unknownType_resolvesCloudFill()
func test_runnableTypes_allReturnTrue()
func test_appInsights_isNotRunnable()
```

### PortalURL

```swift
func test_resourceURL_constructedCorrectly()
func test_resourceGroupURL_constructedCorrectly()
func test_resourceId_notDoublePrefixed()
```

### ARMService (integration, marked @IntegrationTest)

Do not run these in CI by default. Mark with a custom `@IntegrationTest` tag and skip unless `RUN_INTEGRATION_TESTS=1` env var is set.

```swift
// Requires real az login and valid subscription
func test_fetchResourceGroups_returnsResults() async throws
func test_fetchAppState_returnsRunningOrStopped() async throws
```

---

## Test File Structure

```
AzPinTests/
├── AzPinTests.swift            # Test suite entry
├── Mocks/
│   ├── MockARMService.swift
│   ├── MockTokenCache.swift
│   ├── MockPermissionsService.swift
│   └── MockAzCLIService.swift
├── Services/
│   ├── TokenCacheTests.swift
│   └── PermissionsServiceTests.swift
└── Utilities/
    ├── ResourceTypeMapperTests.swift
    └── PortalURLTests.swift
```

---

## In-App Testing (Previews)

Use SwiftUI `#Preview` with mock services injected via environment for visual testing of menu states.

```swift
#Preview("Menu - Running App") {
    MenuBarView()
        .environment(\.armService, MockARMService(appState: .running))
}

#Preview("Menu - Not Signed In") {
    MenuBarView()
        .environment(\.authState, .notSignedIn)
}

#Preview("Menu - Loading") {
    MenuBarView()
        .environment(\.armService, MockARMService(appState: .loading))
}
```

This is the primary way to verify menubar UI states without needing a real Azure account during development.

---

## What Not To Test

- SwiftUI view layout — use Previews
- SwiftData model field definitions — trust the compiler
- `ShellRunner` directly — it wraps `Process`, test integration via `AzCLIService` integration tests only
- The `az` CLI itself — that is Microsoft's responsibility
