namespace AzPin.Windows.Utilities;

public static class PortalUrl
{
    public static Uri ForResource(string armResourceId) =>
        new($"https://portal.azure.com/#resource{armResourceId}");

    public static Uri ForResourceGroup(string subscriptionId, string rgName) =>
        new($"https://portal.azure.com/#resource/subscriptions/{subscriptionId}/resourceGroups/{rgName}");
}
