import Foundation

enum ResourceTypeMapper {
    static let runnableTypes: Set<String> = [
        "microsoft.web/sites",
        "microsoft.web/sites/slots",
        "microsoft.app/containerapps",
        "microsoft.logic/workflows"
    ]

    static func symbolName(for resourceType: String) -> String {
        switch resourceType.lowercased() {
        case "microsoft.web/sites":                        return "globe"
        case "microsoft.insights/components":              return "lightbulb.fill"
        case "microsoft.storage/storageaccounts":          return "externaldrive.fill"
        case "microsoft.servicebus/namespaces":            return "arrow.triangle.branch"
        case "microsoft.keyvault/vaults":                  return "key.fill"
        case "microsoft.apimanagement/service":            return "antenna.radiowaves.left.and.right"
        case "microsoft.sql/servers",
             "microsoft.documentdb/databaseaccounts":      return "cylinder.fill"
        case "microsoft.app/containerapps":                return "shippingbox.fill"
        case "microsoft.web/serverfarms":                  return "bolt.fill"
        default:                                           return "cloud.fill"
        }
    }

    static func isRunnable(_ resourceType: String) -> Bool {
        runnableTypes.contains(resourceType.lowercased())
    }
}
