using System.ComponentModel.DataAnnotations;

namespace AzPin.Windows.Models.Entities;

public class HiddenSubscription
{
    [Key]
    public string SubscriptionId { get; set; } = string.Empty;
}
