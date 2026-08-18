using Xunit;

namespace Lightforge.Tests;

public class LightforgeProjectTests : IDisposable
{
    private readonly string _tempDir;

    public LightforgeProjectTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "lightforge-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Create_MakesProjectDirectory()
    {
        var project = LightforgeProject.Create(_tempDir, "TestProject");

        Assert.True(Directory.Exists(project.ProjectDir));
        Assert.True(File.Exists(project.ProjectFile));
        Assert.Equal("TestProject", project.Name);
    }

    [Fact]
    public void Create_MakesAllFolders()
    {
        var project = LightforgeProject.Create(_tempDir, "TestProject");

        foreach (var folder in LightforgeProject.Folders)
            Assert.True(Directory.Exists(Path.Combine(project.ProjectDir, folder)),
                $"Folder {folder} was not created");
    }

    [Fact]
    public void Create_SetsDefaults()
    {
        var project = LightforgeProject.Create(_tempDir, "TestProject");

        Assert.Equal("WotLK 3.3.5a", project.Expansion);
        Assert.NotEqual(default, project.Created);
        Assert.NotEqual(default, project.LastOpened);
    }

    [Fact]
    public void Open_RoundTrips()
    {
        var created = LightforgeProject.Create(_tempDir, "RoundTrip");
        var opened = LightforgeProject.Open(created.ProjectFile);

        Assert.NotNull(opened);
        Assert.Equal("RoundTrip", opened!.Name);
        Assert.Equal(created.Expansion, opened.Expansion);
        Assert.Equal(created.ProjectDir, opened.ProjectDir);
    }

    [Fact]
    public void Open_ReturnsNullForMissingFile()
    {
        var result = LightforgeProject.Open(Path.Combine(_tempDir, "nonexistent.project"));
        Assert.Null(result);
    }

    [Fact]
    public void Folders_ContainsExpectedEntries()
    {
        Assert.Contains("Maps", LightforgeProject.Folders);
        Assert.Contains("DBCs", LightforgeProject.Folders);
        Assert.Contains("Models", LightforgeProject.Folders);
        Assert.Contains("Textures", LightforgeProject.Folders);
        Assert.Contains("Patches", LightforgeProject.Folders);
        Assert.Contains("SQL", LightforgeProject.Folders);
        Assert.Contains("Lua", LightforgeProject.Folders);
    }
}
