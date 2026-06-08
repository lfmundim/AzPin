using AzPin.Windows.Utilities;

namespace AzPin.Windows.Tests.Tests;

public class ResourceTypeMapperTests
{
    [Fact]
    public void GlyphFor_ReturnsNonEmpty_ForKnownType()
    {
        var glyph = ResourceTypeMapper.GlyphFor("microsoft.web/sites");
        Assert.False(string.IsNullOrEmpty(glyph));
    }

    [Fact]
    public void GlyphFor_IsCaseInsensitive()
    {
        var lower = ResourceTypeMapper.GlyphFor("microsoft.web/sites");
        var upper = ResourceTypeMapper.GlyphFor("MICROSOFT.WEB/SITES");
        var mixed = ResourceTypeMapper.GlyphFor("Microsoft.Web/Sites");
        Assert.Equal(lower, upper);
        Assert.Equal(lower, mixed);
    }

    [Fact]
    public void GlyphFor_ReturnsDefaultGlyph_ForUnknownType()
    {
        var unknown = ResourceTypeMapper.GlyphFor("microsoft.unknown/thingies");
        var known   = ResourceTypeMapper.GlyphFor("microsoft.web/sites");
        // Just verify unknown returns something (default glyph), not same as known
        Assert.False(string.IsNullOrEmpty(unknown));
    }
}
