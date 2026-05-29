# Skill: SwiftData Patterns

Use this file when working with any SwiftData model, setting up the model container, writing queries, or migrating the schema. SwiftData is relatively new and LLM training data skews toward CoreData patterns — this file documents the correct SwiftData approach for AzPin specifically.

---

## Container Setup

The model container is configured once at the app entry point and injected into the SwiftUI environment.

```swift
@main
struct AzPinApp: App {
    let container: ModelContainer

    init() {
        do {
            container = try ModelContainer(
                for: PinnedResourceGroup.self,
                     PinnedResource.self,
                     CachedToken.self
            )
        } catch {
            fatalError("Failed to initialize ModelContainer: \(error)")
        }
    }

    var body: some Scene {
        MenuBarExtra("AzPin", systemImage: "cloud.fill") {
            MenuBarView()
        }
        .modelContainer(container)

        Window("AzPin", id: "main") {
            MainAppView()
        }
        .modelContainer(container)

        Settings {
            SettingsView()
        }
        .modelContainer(container)
    }
}
```

All three scenes share the same `container` instance. Do not create separate containers per scene.

---

## Models

```swift
import SwiftData
import Foundation

@Model
final class PinnedResourceGroup {
    @Attribute(.unique) var id: String      // Full ARM resource group ID
    var subscriptionId: String
    var name: String
    var displayOrder: Int
    @Relationship(deleteRule: .cascade) var resources: [PinnedResource]

    init(id: String, subscriptionId: String, name: String, displayOrder: Int) {
        self.id = id
        self.subscriptionId = subscriptionId
        self.name = name
        self.displayOrder = displayOrder
        self.resources = []
    }
}

@Model
final class PinnedResource {
    @Attribute(.unique) var id: String      // Full ARM resource ID
    var name: String
    var type: String                        // Always store lowercased
    var resourceGroup: String
    var subscriptionId: String
    var location: String
    var displayOrder: Int

    init(id: String, name: String, type: String, resourceGroup: String,
         subscriptionId: String, location: String, displayOrder: Int) {
        self.id = id
        self.name = name
        self.type = type.lowercased()       // Enforce at write time
        self.resourceGroup = resourceGroup
        self.subscriptionId = subscriptionId
        self.location = location
        self.displayOrder = displayOrder
    }
}

@Model
final class CachedToken {
    @Attribute(.unique) var subscriptionId: String
    var tenantId: String
    var accessToken: String
    var expiresOn: Date

    init(subscriptionId: String, tenantId: String, accessToken: String, expiresOn: Date) {
        self.subscriptionId = subscriptionId
        self.tenantId = tenantId
        self.accessToken = accessToken
        self.expiresOn = expiresOn
    }
}
```

**Rules:**
- Models are `final class`, not struct.
- `@Attribute(.unique)` on natural unique keys (`id`, `subscriptionId` for tokens).
- `@Relationship(deleteRule: .cascade)` on `PinnedResourceGroup.resources` so deleting an RG cascades to its resources.
- ARM type strings are always lowercased at write time in the `PinnedResource` initializer.
- Do not add `Codable` conformance to SwiftData models. Keep ARM response structs and persistence models separate types.

---

## Querying

Use `@Query` in views for reactive data. Use `ModelContext` directly in services.

### In Views

```swift
struct SidebarView: View {
    @Query(sort: \PinnedResourceGroup.displayOrder) var pinnedGroups: [PinnedResourceGroup]

    var body: some View {
        List(pinnedGroups) { group in
            // ...
        }
    }
}
```

### In Services (via ModelContext)

Services receive `ModelContext` via dependency injection, not via `@Environment`. Pass it in during initialization or as a parameter.

```swift
// Fetch all pinned RGs
let descriptor = FetchDescriptor<PinnedResourceGroup>(
    sortBy: [SortDescriptor(\.displayOrder)]
)
let groups = try context.fetch(descriptor)

// Fetch token for a subscription
let subscriptionId = "..."
let descriptor = FetchDescriptor<CachedToken>(
    predicate: #Predicate { $0.subscriptionId == subscriptionId }
)
let tokens = try context.fetch(descriptor)
let token = tokens.first
```

### Insert

```swift
let group = PinnedResourceGroup(id: id, subscriptionId: sub, name: name, displayOrder: nextOrder)
context.insert(group)
try context.save()
```

### Delete

```swift
context.delete(group)
try context.save()
```

Cascade delete is configured on `PinnedResourceGroup.resources` — deleting a group automatically deletes its child resources.

### Upsert Pattern (for CachedToken)

```swift
func upsertToken(_ response: AzureTokenResponse, context: ModelContext) throws {
    let subscriptionId = response.subscription
    let descriptor = FetchDescriptor<CachedToken>(
        predicate: #Predicate { $0.subscriptionId == subscriptionId }
    )
    let existing = try context.fetch(descriptor).first

    if let existing {
        existing.accessToken = response.accessToken
        existing.expiresOn = response.parsedExpiresOn
        existing.tenantId = response.tenant
    } else {
        let token = CachedToken(
            subscriptionId: subscriptionId,
            tenantId: response.tenant,
            accessToken: response.accessToken,
            expiresOn: response.parsedExpiresOn
        )
        context.insert(token)
    }
    try context.save()
}
```

---

## Reordering (displayOrder)

`displayOrder` is an `Int` managed manually. When the user reorders items:

```swift
func move(groups: [PinnedResourceGroup], from source: IndexSet, to destination: Int, context: ModelContext) throws {
    var reordered = groups
    reordered.move(fromOffsets: source, toOffset: destination)
    for (index, group) in reordered.enumerated() {
        group.displayOrder = index
    }
    try context.save()
}
```

---

## Migration

AzPin uses lightweight migration via `ModelContainer` versioning. When adding fields in v1.1+:

```swift
enum AzPinSchemaV1: VersionedSchema {
    static var versionIdentifier = Schema.Version(1, 0, 0)
    static var models: [any PersistentModel.Type] = [
        PinnedResourceGroup.self, PinnedResource.self, CachedToken.self
    ]
}
```

For v1.1 multi-subscription additions, new optional fields on `PinnedResourceGroup` (`subscriptionDisplayName`) are additive and do not require a migration plan — SwiftData handles nil-defaulting new optional properties automatically.

For v2 environment support, a new `PinnedEnvironment` model will be added and `PinnedResourceGroup` will gain a nullable relationship. This will require a `MigrationPlan` — document it in this file when the time comes.

---

## What Not To Do

- Do not use `NSManagedObject` or any CoreData API.
- Do not use `UserDefaults` for structured data — only for simple boolean flags like `hasCompletedOnboarding`.
- Do not store ARM resource list data in SwiftData. Only pinned selections and tokens are persisted.
- Do not call `context.save()` inside a tight loop — batch changes and save once.
- Do not add `@Model` to ARM response structs. They are plain `Decodable` structs, not persistent models.
