using AzPin.Windows.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AzPin.Windows.Data;

public class AzPinDbContext(DbContextOptions<AzPinDbContext> options) : DbContext(options)
{
    public DbSet<PinnedResourceGroup> PinnedResourceGroups => Set<PinnedResourceGroup>();
    public DbSet<PinnedResource> PinnedResources => Set<PinnedResource>();
    public DbSet<CachedToken> CachedTokens => Set<CachedToken>();
}
