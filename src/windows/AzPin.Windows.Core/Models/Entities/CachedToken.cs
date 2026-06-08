using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Models.Entities;

[Index(nameof(SubscriptionId), IsUnique = true)]
public class CachedToken
{
    [Key]
    public int LocalId { get; set; }
    public string SubscriptionId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
}
