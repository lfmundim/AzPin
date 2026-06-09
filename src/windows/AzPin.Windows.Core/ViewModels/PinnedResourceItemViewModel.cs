using AzPin.Windows.Models.Entities;
using AzPin.Windows.Utilities;

namespace AzPin.Windows.ViewModels;

public class PinnedResourceItemViewModel(PinnedResource resource)
{
    public int LocalId => resource.LocalId;
    public string ResourceId => resource.Id;
    public string Name => resource.Name;
    public string Type => resource.Type;
    public string GlyphCode => ResourceTypeMapper.GlyphFor(resource.Type);
    public Uri PortalUri => PortalUrl.ForResource(resource.Id);
}
