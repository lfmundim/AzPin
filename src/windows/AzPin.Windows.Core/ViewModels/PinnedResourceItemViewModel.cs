using AzPin.Windows.Models.Entities;
using AzPin.Windows.Utilities;

namespace AzPin.Windows.ViewModels;

public class PinnedResourceItemViewModel(PinnedResource resource)
{
    public string Name => resource.Name;
    public string GlyphCode => ResourceTypeMapper.GlyphFor(resource.Type);
    public Uri PortalUri => PortalUrl.ForResource(resource.Id);
}
