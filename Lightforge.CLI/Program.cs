using System.Diagnostics;
using System.Text.Json;

namespace Lightforge;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 0;
        }

        var sub = args.Skip(1).ToArray();
        return args[0].ToLowerInvariant() switch
        {
            "tools" or "list" => ListTools(sub),
            "search" => SearchTools(sub),
            "launch" => LaunchTool(sub),
            "new" => NewProject(sub),
            "info" => ProjectInfo(sub),
            "versions" => ListVersions(),
            "validate" => ValidateTools(sub),
            "dbc-info" => DbcTool.Info(sub),
            "dbc-diff" => DbcTool.Diff(sub),
            "blp-info" => BlpTool.Info(sub),
            "adt-info" => AdtTool.Info(sub),
            "listfile" => ListfileTool.Search(sub),
            "sql-gen" => SqlGenTool.Generate(sub),
            "help" or "--help" or "-h" => ShowHelp(args.Skip(1).FirstOrDefault()),
            "version" or "--version" or "-v" => ShowVersion(),
            _ => UnknownCommand(args[0])
        };
    }

    static void PrintUsage()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("lightforge");
        Console.ResetColor();
        Console.WriteLine(" - WoW modding tool launcher CLI\n");
        Console.WriteLine("Usage: lightforge <command> [options]\n");
        Console.WriteLine("Commands:");
        PrintCommand("tools", "List all registered tools (filter by version/category)");
        PrintCommand("search <query>", "Search tools by name or description");
        PrintCommand("launch <name>", "Launch a tool by name");
        PrintCommand("new <name> [path]", "Create a new modding project");
        PrintCommand("info [project]", "Show project details");
        PrintCommand("versions", "List all supported WoW versions");
        PrintCommand("validate [path]", "Check which tools have executables present");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n  Lightforge Tools:");
        Console.ResetColor();
        PrintCommand("dbc-info <file>", "Inspect DBC/DB2 file header and structure");
        PrintCommand("dbc-diff <a> <b>", "Compare two DBC files row-by-row");
        PrintCommand("blp-info <file|dir>", "Read BLP texture headers (batch supported)");
        PrintCommand("adt-info <file>", "Inspect ADT map tile chunks and assets");
        PrintCommand("listfile <pattern>", "Search the community listfile for assets");
        PrintCommand("sql-gen [template]", "Generate SQL templates for WoW databases");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine();
        Console.ResetColor();
        PrintCommand("help [command]", "Show detailed help for a command");
        Console.WriteLine($"\n{WowToolRegistry.GetAllTools().Length} tools registered, " +
            $"Vanilla 1.12.1 through BfA 8.3.7");
    }

    static void PrintCommand(string name, string desc)
    {
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(name.PadRight(22));
        Console.ResetColor();
        Console.WriteLine(desc);
    }

    static int ListTools(string[] args)
    {
        var version = WowVersion.All;
        string? category = null;
        string? status = null;
        bool compact = false;
        bool json = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--version" or "-v" when i + 1 < args.Length:
                    version = ParseVersion(args[++i]);
                    if (version == (WowVersion)(-1)) return 1;
                    break;
                case "--category" or "-c" when i + 1 < args.Length:
                    category = args[++i].ToUpperInvariant();
                    break;
                case "--status" or "-s" when i + 1 < args.Length:
                    status = args[++i];
                    break;
                case "--compact":
                    compact = true;
                    break;
                case "--json":
                    json = true;
                    break;
            }
        }

        var tools = WowToolRegistry.GetAllTools()
            .Where(t => t.IsCompatible(version))
            .Where(t => category == null || t.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .Where(t => status == null || t.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (json)
        {
            var output = tools.Select(t => new
            {
                t.Name, t.Description, t.Category, t.Status, t.Source,
                Versions = t.Compatible.Select(v => v.ToString()).ToArray()
            });
            Console.WriteLine(JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (compact)
        {
            foreach (var t in tools)
                Console.WriteLine($"{t.Name,-28} {t.Category,-16} {FormatVersions(t.Compatible)}");
            return 0;
        }

        var grouped = tools.GroupBy(t => t.Category).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n  {group.Key}");
            Console.ResetColor();

            foreach (var tool in group)
            {
                Console.Write("    ");
                Console.ForegroundColor = StatusColor(tool.Status);
                Console.Write("●");
                Console.ResetColor();
                Console.Write($" {tool.Name,-24} ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(tool.Description);
                Console.ResetColor();
                Console.WriteLine();
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {tools.Length} tools" +
            (version != WowVersion.All ? $" for {WowVersionInfo.DisplayName(version)}" : "") +
            (category != null ? $" in {category}" : ""));
        Console.ResetColor();
        return 0;
    }

    static int SearchTools(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge search <query>");
            return 1;
        }

        var query = string.Join(" ", args).ToLowerInvariant();
        var matches = WowToolRegistry.GetAllTools()
            .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            Console.WriteLine($"No tools matching \"{query}\"");
            return 0;
        }

        foreach (var t in matches)
        {
            Console.ForegroundColor = StatusColor(t.Status);
            Console.Write("●");
            Console.ResetColor();
            Console.Write($" {t.Name,-24} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{t.Category,-16} ");
            Console.ResetColor();
            Console.WriteLine(t.Description);

            if (!string.IsNullOrEmpty(t.Source))
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"  {t.Source}");
                Console.ResetColor();
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Versions: {FormatVersions(t.Compatible)}");
            Console.ResetColor();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n{matches.Length} result{(matches.Length == 1 ? "" : "s")}");
        Console.ResetColor();
        return 0;
    }

    static int LaunchTool(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge launch <tool-name>");
            return 1;
        }

        var name = string.Join(" ", args);
        var tool = WowToolRegistry.GetAllTools()
            .FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (tool == null)
        {
            var close = WowToolRegistry.GetAllTools()
                .Where(t => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (close.Length > 0)
            {
                Console.Error.WriteLine($"Tool \"{name}\" not found. Did you mean:");
                foreach (var t in close)
                    Console.Error.WriteLine($"  {t.Name}");
                return 1;
            }

            Console.Error.WriteLine($"Tool \"{name}\" not found. Use 'lightforge tools' to list all.");
            return 1;
        }

        var toolsDir = Path.Combine(AppContext.BaseDirectory, "Toolset Binaries");
        var toolPath = Path.Combine(toolsDir, tool.RelativePath);

        if (Directory.Exists(toolPath))
        {
            var exe = Directory.GetFiles(toolPath, "*.exe").FirstOrDefault();
            if (exe != null)
            {
                Console.WriteLine($"Launching {tool.Name}...");
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
                return 0;
            }
        }

        if (File.Exists(toolPath + ".exe"))
        {
            Console.WriteLine($"Launching {tool.Name}...");
            Process.Start(new ProcessStartInfo(toolPath + ".exe") { UseShellExecute = true });
            return 0;
        }

        Console.Error.WriteLine($"Executable not found at: {toolPath}");
        Console.Error.WriteLine($"Place the tool in: Toolset Binaries\\{tool.RelativePath}");
        return 1;
    }

    static int NewProject(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge new <name> [parent-directory]");
            return 1;
        }

        var name = args[0];
        var parentDir = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();

        if (!Directory.Exists(parentDir))
        {
            Console.Error.WriteLine($"Directory not found: {parentDir}");
            return 1;
        }

        var projectDir = Path.Combine(parentDir, name);
        if (Directory.Exists(projectDir))
        {
            Console.Error.WriteLine($"Directory already exists: {projectDir}");
            return 1;
        }

        var project = LightforgeProject.Create(parentDir, name);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Created");
        Console.ResetColor();
        Console.WriteLine($" project \"{name}\" at {project.ProjectDir}\n");

        Console.WriteLine("Folders:");
        foreach (var folder in LightforgeProject.Folders)
            Console.WriteLine($"  {folder}/");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\nProject file: {project.ProjectFile}");
        Console.ResetColor();
        return 0;
    }

    static int ProjectInfo(string[] args)
    {
        string? projectFile = null;

        if (args.Length > 0)
        {
            projectFile = args[0];
            if (Directory.Exists(projectFile))
                projectFile = Path.Combine(projectFile, "lightforge.project");
        }
        else
        {
            projectFile = FindProjectFile(Directory.GetCurrentDirectory());
        }

        if (projectFile == null || !File.Exists(projectFile))
        {
            Console.Error.WriteLine("No lightforge.project found. Specify a path or run from a project directory.");
            return 1;
        }

        var project = LightforgeProject.Open(projectFile);
        if (project == null)
        {
            Console.Error.WriteLine($"Failed to parse: {projectFile}");
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {project.Name}");
        Console.ResetColor();

        PrintField("Expansion", project.Expansion);
        PrintField("Client Path", string.IsNullOrEmpty(project.ClientPath) ? "(not set)" : project.ClientPath);
        PrintField("Created", project.Created.ToString("yyyy-MM-dd HH:mm"));
        PrintField("Last Opened", project.LastOpened.ToString("yyyy-MM-dd HH:mm"));
        PrintField("Location", project.ProjectDir);

        Console.WriteLine();
        var dirs = LightforgeProject.Folders
            .Select(f => Path.Combine(project.ProjectDir, f))
            .Where(Directory.Exists);

        foreach (var dir in dirs)
        {
            var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
            var size = files.Sum(f => new FileInfo(f).Length);
            var dirName = Path.GetFileName(dir);
            Console.Write($"  {dirName,-12} ");
            Console.ForegroundColor = files.Length > 0 ? ConsoleColor.White : ConsoleColor.DarkGray;
            Console.Write($"{files.Length} files");
            if (size > 0)
                Console.Write($"  ({FormatSize(size)})");
            Console.ResetColor();
            Console.WriteLine();
        }

        return 0;
    }

    static int ListVersions()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Supported WoW Versions\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {"Version",-20} {"Build",-8} {"Format",-6} Tools");
        Console.ResetColor();

        foreach (var (version, display, build) in WowVersionInfo.Versions)
        {
            if (version == WowVersion.All) continue;
            var count = WowToolRegistry.GetAllTools().Count(t => t.Compatible.Contains(version));
            var format = WowVersionInfo.UsesMpq(version) ? "MPQ" : "CASC";

            Console.Write($"  {display,-20} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{build,-8} ");
            Console.ForegroundColor = WowVersionInfo.UsesMpq(version) ? ConsoleColor.Blue : ConsoleColor.Magenta;
            Console.Write($"{format,-6} ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{count}");
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {WowToolRegistry.GetAllTools().Length} total tools registered");
        Console.ResetColor();
        return 0;
    }

    static int ValidateTools(string[] args)
    {
        var toolsDir = args.Length > 0 ? args[0] :
            Path.Combine(AppContext.BaseDirectory, "Toolset Binaries");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Validating tools in: {toolsDir}\n");
        Console.ResetColor();

        int found = 0, missing = 0;
        foreach (var tool in WowToolRegistry.GetAllTools())
        {
            var path = Path.Combine(toolsDir, tool.RelativePath);
            bool exists = false;

            if (Directory.Exists(path))
                exists = Directory.GetFiles(path, "*.exe").Length > 0;
            else if (File.Exists(path + ".exe"))
                exists = true;

            if (exists)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  FOUND   ");
                found++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("  MISSING ");
                missing++;
            }

            Console.ResetColor();
            Console.WriteLine(tool.Name);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {found} found, {missing} missing of {found + missing} total");
        Console.ResetColor();
        return missing > 0 ? 1 : 0;
    }

    static int ShowHelp(string? command)
    {
        switch (command?.ToLowerInvariant())
        {
            case "tools" or "list":
                Console.WriteLine("lightforge tools [options]\n");
                Console.WriteLine("Options:");
                Console.WriteLine("  --version, -v <ver>   Filter by WoW version (vanilla, tbc, wotlk, cata, mop, wod, legion, bfa)");
                Console.WriteLine("  --category, -c <cat>  Filter by category");
                Console.WriteLine("  --status, -s <stat>   Filter by status (Active, Maintained, Stale, etc.)");
                Console.WriteLine("  --compact             One-line-per-tool format");
                Console.WriteLine("  --json                Output as JSON");
                Console.WriteLine("\nExamples:");
                Console.WriteLine("  lightforge tools --version wotlk");
                Console.WriteLine("  lightforge tools --category \"DATA EDITING\" --compact");
                Console.WriteLine("  lightforge tools --json > tools.json");
                break;
            case "search":
                Console.WriteLine("lightforge search <query>\n");
                Console.WriteLine("Search tools by name, description, or category.\n");
                Console.WriteLine("Examples:");
                Console.WriteLine("  lightforge search mpq");
                Console.WriteLine("  lightforge search \"spell editor\"");
                break;
            case "launch":
                Console.WriteLine("lightforge launch <tool-name>\n");
                Console.WriteLine("Launch a tool by its registered name. Case-insensitive.\n");
                Console.WriteLine("Examples:");
                Console.WriteLine("  lightforge launch Noggit3");
                Console.WriteLine("  lightforge launch \"MPQ Editor\"");
                break;
            case "new":
                Console.WriteLine("lightforge new <name> [parent-directory]\n");
                Console.WriteLine("Create a new modding project with standard folders.\n");
                Console.WriteLine("Examples:");
                Console.WriteLine("  lightforge new MyMod");
                Console.WriteLine("  lightforge new CustomServer D:\\Projects");
                break;
            case "validate":
                Console.WriteLine("lightforge validate [tools-directory]\n");
                Console.WriteLine("Check which registered tools have executables present.\n");
                Console.WriteLine("Examples:");
                Console.WriteLine("  lightforge validate");
                Console.WriteLine("  lightforge validate \"D:\\Toolset Binaries\"");
                break;
            default:
                PrintUsage();
                break;
        }
        return 0;
    }

    static int ShowVersion()
    {
        Console.WriteLine("lightforge 2.0.0");
        return 0;
    }

    static int UnknownCommand(string cmd)
    {
        Console.Error.WriteLine($"Unknown command: {cmd}");
        Console.Error.WriteLine("Run 'lightforge help' for usage.");
        return 1;
    }

    static WowVersion ParseVersion(string input)
    {
        return input.ToLowerInvariant() switch
        {
            "vanilla" or "classic" or "1.12" or "1.12.1" => WowVersion.Vanilla,
            "tbc" or "bc" or "2.4" or "2.4.3" => WowVersion.TBC,
            "wotlk" or "wrath" or "3.3.5" or "3.3.5a" => WowVersion.WotLK,
            "cata" or "cataclysm" or "4.3" or "4.3.4" => WowVersion.Cata,
            "mop" or "pandaria" or "5.4" or "5.4.8" => WowVersion.MoP,
            "wod" or "draenor" or "6.2" or "6.2.4" => WowVersion.WoD,
            "legion" or "7.3" or "7.3.5" => WowVersion.Legion,
            "bfa" or "8.3" or "8.3.7" => WowVersion.BfA,
            "all" => WowVersion.All,
            _ => InvalidVersion(input)
        };
    }

    static WowVersion InvalidVersion(string input)
    {
        Console.Error.WriteLine($"Unknown version: {input}");
        Console.Error.WriteLine("Valid: vanilla, tbc, wotlk, cata, mop, wod, legion, bfa, all");
        return (WowVersion)(-1);
    }

    static string FormatVersions(WowVersion[] versions)
    {
        if (versions.Length == 8) return "All";
        return string.Join(", ", versions.Select(v => v.ToString()));
    }

    static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };

    static ConsoleColor StatusColor(string status) => status.ToLowerInvariant() switch
    {
        "active" or "complete" => ConsoleColor.Green,
        "maintained" or "stable" => ConsoleColor.Blue,
        "stale" or "dormant" => ConsoleColor.DarkYellow,
        "archived" or "dead" => ConsoleColor.Red,
        _ => ConsoleColor.Gray
    };

    static void PrintField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {label + ":",-16}");
        Console.ResetColor();
        Console.WriteLine(value);
    }

    static string? FindProjectFile(string dir)
    {
        var file = Path.Combine(dir, "lightforge.project");
        if (File.Exists(file)) return file;

        var parent = Directory.GetParent(dir)?.FullName;
        if (parent != null && parent != dir)
            return FindProjectFile(parent);
        return null;
    }
}
