using System.ComponentModel.DataAnnotations;

namespace AzPin.Windows.Models.Entities;

public class PinnedResource
{
    [Key]
    public int LocalId { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ResourceGroup { get; set; } = string.Empty;
    public string SubscriptionId { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public int? PinnedResourceGroupLocalId { get; set; }
    public PinnedResourceGroup? PinnedResourceGroupEntity { get; set; }
}
