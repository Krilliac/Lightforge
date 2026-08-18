using Xunit;

namespace Lightforge.Tests;

public class WowToolRegistryTests
{
    [Fact]
    public void GetAllTools_ReturnsNonEmpty()
    {
        var tools = WowToolRegistry.GetAllTools();
        Assert.NotEmpty(tools);
        Assert.True(tools.Length >= 60, $"Expected at least 60 tools, got {tools.Length}");
    }

    [Fact]
    public void AllTools_HaveRequiredFields()
    {
        foreach (var tool in WowToolRegistry.GetAllTools())
        {
            Assert.False(string.IsNullOrWhiteSpace(tool.Name), "Tool name is empty");
            Assert.False(string.IsNullOrWhiteSpace(tool.Description), $"{tool.Name} has empty description");
            Assert.False(string.IsNullOrWhiteSpace(tool.RelativePath), $"{tool.Name} has empty path");
            Assert.False(string.IsNullOrWhiteSpace(tool.Category), $"{tool.Name} has empty category");
            Assert.NotEmpty(tool.Compatible);
        }
    }

    [Fact]
    public void AllTools_HaveValidCategories()
    {
        var validCategories = WowToolRegistry.CategoryOrder;
        foreach (var tool in WowToolRegistry.GetAllTools())
            Assert.Contains(tool.Category, validCategories);
    }

    [Fact]
    public void AllTools_HaveUniqueNames()
    {
        var tools = WowToolRegistry.GetAllTools();
        var names = tools.Select(t => t.Name).ToList();
        var dupes = names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(dupes);
    }

    [Theory]
    [InlineData(WowVersion.Vanilla)]
    [InlineData(WowVersion.WotLK)]
    [InlineData(WowVersion.BfA)]
    public void GetToolsForVersion_ReturnsSubset(WowVersion version)
    {
        var all = WowToolRegistry.GetAllTools();
        var filtered = WowToolRegistry.GetToolsForVersion(version);

        Assert.NotEmpty(filtered);
        Assert.True(filtered.Length <= all.Length);
        Assert.All(filtered, t => Assert.Contains(version, t.Compatible));
    }

    [Fact]
    public void GetToolsForVersion_All_ReturnsFull()
    {
        var all = WowToolRegistry.GetAllTools();
        var fromAll = WowToolRegistry.GetToolsForVersion(WowVersion.All);
        Assert.Equal(all.Length, fromAll.Length);
    }

    [Fact]
    public void GetGroupedTools_GroupsByCategory()
    {
        var grouped = WowToolRegistry.GetGroupedTools(WowVersion.All);
        Assert.NotEmpty(grouped);
        Assert.All(grouped, g =>
        {
            Assert.False(string.IsNullOrWhiteSpace(g.category));
            Assert.NotEmpty(g.tools);
        });
    }

    [Fact]
    public void ToolEntry_IsCompatible_MatchesCorrectly()
    {
        var tool = new ToolEntry("Test", "desc", "path", "CAT",
            [WowVersion.Vanilla, WowVersion.TBC, WowVersion.WotLK]);

        Assert.True(tool.IsCompatible(WowVersion.Vanilla));
        Assert.True(tool.IsCompatible(WowVersion.WotLK));
        Assert.True(tool.IsCompatible(WowVersion.All));
        Assert.False(tool.IsCompatible(WowVersion.Cata));
        Assert.False(tool.IsCompatible(WowVersion.BfA));
    }

    [Fact]
    public void WotLK_HasMostTools()
    {
        var counts = new Dictionary<WowVersion, int>();
        foreach (var (version, _, _) in WowVersionInfo.Versions)
        {
            if (version == WowVersion.All) continue;
            counts[version] = WowToolRegistry.GetToolsForVersion(version).Length;
        }

        var max = counts.MaxBy(kv => kv.Value);
        Assert.Equal(WowVersion.WotLK, max.Key);
    }

    [Theory]
    [InlineData(WowVersion.Vanilla, true)]
    [InlineData(WowVersion.WotLK, true)]
    [InlineData(WowVersion.Cata, true)]
    [InlineData(WowVersion.MoP, true)]
    [InlineData(WowVersion.WoD, false)]
    [InlineData(WowVersion.Legion, false)]
    [InlineData(WowVersion.BfA, false)]
    public void UsesMpq_CorrectForVersion(WowVersion version, bool expected)
    {
        Assert.Equal(expected, WowVersionInfo.UsesMpq(version));
    }

    [Theory]
    [InlineData(WowVersion.WoD, true)]
    [InlineData(WowVersion.Legion, true)]
    [InlineData(WowVersion.BfA, true)]
    [InlineData(WowVersion.WotLK, false)]
    [InlineData(WowVersion.Vanilla, false)]
    public void UsesCasc_CorrectForVersion(WowVersion version, bool expected)
    {
        Assert.Equal(expected, WowVersionInfo.UsesCasc(version));
    }
}
