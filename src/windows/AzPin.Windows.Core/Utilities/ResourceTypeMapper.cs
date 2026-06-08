namespace AzPin.Windows.Utilities;

public static class ResourceTypeMapper
{
    private static readonly Dictionary<string, string> TypeToGlyph =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["microsoft.resources/resourcegroups"]   = "", // Folder
        ["microsoft.web/sites"]                  = "", // Globe
        ["microsoft.web/sites/slots"]            = "",
        ["microsoft.insights/components"]        = "", // Lightbulb
        ["microsoft.storage/storageaccounts"]    = "", // Storage
        ["microsoft.servicebus/namespaces"]      = "", // Branch
        ["microsoft.keyvault/vaults"]            = "", // Key
        ["microsoft.apimanagement/service"]      = "", // Antenna
        ["microsoft.sql/servers"]                = "", // Database
        ["microsoft.documentdb/databaseaccounts"] = "",
        ["microsoft.app/containerapps"]          = "", // Box/Package
        ["microsoft.logic/workflows"]            = "", // Flow
    };

    private const string DefaultGlyph = ""; // Cloud

    public static string GlyphFor(string resourceType) =>
        TypeToGlyph.GetValueOrDefault(resourceType.ToLowerInvariant(), DefaultGlyph);
}
