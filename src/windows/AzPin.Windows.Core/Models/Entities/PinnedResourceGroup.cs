using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Models.Entities;

[Index(nameof(SubscriptionId), nameof(Name), IsUnique = true)]
public class PinnedResourceGroup
{
    public int LocalId { get; set; }
    public string Id { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public ICollection<PinnedResource> Resources { get; set; } = [];
}
