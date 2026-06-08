using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Tests;

public class PortalUrlTests
{
    [Fact]
    public void ForResource_ContainsResourceId()
    {
        var uri = PortalUrl.ForResource("/subscriptions/s1/resourceGroups/rg1/providers/Microsoft.Web/sites/myapp");
        Assert.Contains("/subscriptions/s1", uri.ToString());
        Assert.Contains("myapp", uri.ToString());
    }

    [Fact]
    public void ForResourceGroup_ContainsSubscriptionAndName()
    {
        var uri = PortalUrl.ForResourceGroup("sub-123", "my-rg");
        var s = uri.ToString();
        Assert.Contains("sub-123", s);
        Assert.Contains("my-rg", s);
        Assert.StartsWith("https://portal.azure.com", s);
    }
}
