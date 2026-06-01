using System.Text.Json.Serialization;

namespace AzPin.Windows.Models;

public record AzAccount(
    [property: JsonPropertyName("user")] AzAccountUser? User,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("name")] string Name
);

public record AzAccountUser(
    [property: JsonPropertyName("name")] string Name
);

public record AzSubscription(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("isDefault")] bool IsDefault
);

public record AzTokenResponse(
    [property: JsonPropertyName("accessToken")] string AccessToken,
    [property: JsonPropertyName("expiresOn")] string ExpiresOn,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("subscription")] string Subscription,
    DateTime ExpiresOnUtc
);
