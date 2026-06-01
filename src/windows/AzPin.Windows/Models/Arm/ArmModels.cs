using System.Text.Json.Serialization;

namespace AzPin.Windows.Models.Arm;

public record ArmListResponse<T>(
    [property: JsonPropertyName("value")] List<T> Value
);

public record ArmSubscription(
    [property: JsonPropertyName("subscriptionId")] string SubscriptionId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("tenantId")] string TenantId
);

public record ArmResourceGroup(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("location")] string Location
);

public record ArmResource(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("location")] string Location
);

public record ArmSiteProperties(
    [property: JsonPropertyName("state")] string State
);

public record ArmSiteResponse(
    [property: JsonPropertyName("properties")] ArmSiteProperties? Properties
);
