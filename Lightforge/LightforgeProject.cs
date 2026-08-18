using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lightforge;

public class LightforgeProject
{
    public string Name { get; set; } = "Untitled";
    public string ClientPath { get; set; } = "";
    public string Expansion { get; set; } = "WotLK 3.3.5a";
    public DateTime Created { get; set; } = DateTime.Now;
    public DateTime LastOpened { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string ProjectDir { get; set; } = "";

    [JsonIgnore]
    public string ProjectFile => Path.Combine(ProjectDir, "lightforge.project");

    public static readonly string[] Folders =
    [
        "Maps",
        "DBCs",
        "Models",
        "Textures",
        "Patches",
        "SQL",
        "Lua",
        "Exports",
        "Config"
    ];

    public static LightforgeProject Create(string parentDir, string name)
    {
        var project = new LightforgeProject
        {
            Name = name,
            ProjectDir = Path.Combine(parentDir, name)
        };

        Directory.CreateDirectory(project.ProjectDir);
        foreach (var folder in Folders)
            Directory.CreateDirectory(Path.Combine(project.ProjectDir, folder));

        project.Save();
        return project;
    }

    public static LightforgeProject? Open(string projectFile)
    {
        if (!File.Exists(projectFile)) return null;

        var json = File.ReadAllText(projectFile);
        var project = JsonSerializer.Deserialize<LightforgeProject>(json);
        if (project == null) return null;

        project.ProjectDir = Path.GetDirectoryName(projectFile)!;
        project.LastOpened = DateTime.Now;
        project.Save();
        return project;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(ProjectFile, json);
    }
}

public static class RecentProjects
{
    private static readonly string RecentFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Lightforge", "recent.json");

    public static List<RecentEntry> Load()
    {
        if (!File.Exists(RecentFile)) return [];
        var json = File.ReadAllText(RecentFile);
        return JsonSerializer.Deserialize<List<RecentEntry>>(json) ?? [];
    }

    public static void Add(string projectFile, string name)
    {
        var list = Load();
        list.RemoveAll(e => e.Path == projectFile);
        list.Insert(0, new RecentEntry { Path = projectFile, Name = name, LastOpened = DateTime.Now });
        if (list.Count > 20) list.RemoveRange(20, list.Count - 20);

        var dir = Path.GetDirectoryName(RecentFile)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(RecentFile, JsonSerializer.Serialize(list,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public class RecentEntry
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTime LastOpened { get; set; }
    }
}
