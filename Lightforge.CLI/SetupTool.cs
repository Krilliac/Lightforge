using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Lightforge;

static class SetupTool
{
    record DownloadEntry(
        string ToolName,
        string Owner,
        string Repo,
        string AssetPattern,
        string ExtractTo,
        string? DirectUrl = null);

    static readonly DownloadEntry[] Downloads =
    [
        new("Noggit3",         "wowdev",          "noggit3",           "*noggit*win*",     @"noggit3"),
        new("WDBXEditor",      "WowDevTools",     "WDBXEditor",        "*WDBXEditor*",     @"WDBXEditor"),
        new("WDBXEditor2",     "MaxtorCoder",     "WDBXEditor2",       "*WDBXEditor2*",    @"WDBXEditor2"),
        new("BLPConverter",    "Kanma",           "BLPConverter",      "*BLPConverter*win*",@"BLPConverter"),
        new("CASCExplorer",    "WoW-Tools",       "CASCExplorer",      "*CASCExplorer*",   @"CASCExplorer"),
        new("CASCHost",        "WowDevTools",     "CASCHost",          "*CASCHost*",       @"CASCHost"),
        new("wow.export",      "Kruithne",        "wow.export",        "*wow.export*win*",  @"wowexport"),
        new("WowPacketParser", "TrinityCore",     "WowPacketParser",   "*WowPacketParser*", @"WowPacketParser"),
        new("MultiConverter",  "MaxtorCoder",     "MultiConverter",    "*MultiConverter*",  @"MultiConverter"),
        new("Keira3",          "azerothcore",     "Keira3",            "*keira*win*",       @"Keira3"),
        new("WMVx",            "Frostshake",      "WMVx",              "*WMVx*win*",        @"WMVx"),
        new("mpqcli",          "TheGrayDot",      "mpqcli",            "*mpqcli*win*",      @"mpqcli"),
        new("WoW Database Editor", "BAndysc",     "WoWDatabaseEditor", "*WoWDatabaseEditor*win*", @"WDE"),
        new("M2Mod",           "M2Mod",           "m2mod",             "*m2mod*",           @"M2Mod"),
        new("Spell Editor",    "stoneharry",      "WoW-Spell-Editor",  "*SpellEditor*",    @"SpellEditor"),
        new("TrinityCreator",  "NotCoffee418",    "TrinityCreator",    "*TrinityCreator*",  @"TrinityCreator"),
        new("RCE Patcher",     "Gargash",         "WoW-RCE-Patcher",   "*RCE*",            @"RCEPatcher"),
        new("wow-patcher",     "wowemulation-dev","wow-patcher",       "*wow-patcher*win*", @"wowpatcher"),
        new("Arctium Launcher","Arctium",         "WoW-Launcher",      "*Arctium*",        @"ArctiumLauncher"),
        new("VanillaFixes",    "hannesmann",      "vanillafixes",      "*vanillafixes*",   @"VanillaFixes"),
        new("Map Asset Parser","stoneharry",      "WoW-Map-Asset-Parser","*MapAsset*",     @"MapAssetParser"),
        new("jM2converter",    "WowDevs",         "jM2converter",      "*jM2converter*",   @"jM2converter"),
        new("WoWHeightGen",    "CucFlavius",      "WoWHeightGen",      "*WoWHeightGen*",   @"WoWHeightGen"),

        new("MPQ Editor", "", "", "", @"MPQEditor",
            DirectUrl: "http://www.zezula.net/download/mpqeditor_en.zip"),
    ];

    public static int Run(string[] args)
    {
        string? toolsDir = null;
        bool listOnly = false;
        bool force = false;
        string? single = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--path" or "-p" when i + 1 < args.Length:
                    toolsDir = args[++i];
                    break;
                case "--list" or "-l":
                    listOnly = true;
                    break;
                case "--force" or "-f":
                    force = true;
                    break;
                case "--tool" or "-t" when i + 1 < args.Length:
                    single = args[++i];
                    break;
                case "--help" or "-h":
                    PrintHelp();
                    return 0;
            }
        }

        toolsDir ??= Path.Combine(AppContext.BaseDirectory, "Toolset Binaries");

        if (listOnly)
            return ListAvailable(toolsDir);

        return DownloadTools(toolsDir, force, single).GetAwaiter().GetResult();
    }

    static void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Tool Setup / Downloader\n");
        Console.ResetColor();
        Console.WriteLine("Usage: lightforge setup [options]\n");
        Console.WriteLine("Downloads community tools from GitHub releases into the Toolset Binaries folder.\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --path, -p <dir>    Toolset Binaries directory (default: next to exe)");
        Console.WriteLine("  --list, -l          List downloadable tools and their status");
        Console.WriteLine("  --tool, -t <name>   Download only this tool");
        Console.WriteLine("  --force, -f         Re-download even if already present");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {Downloads.Length} tools have automated downloads configured.");
        Console.ResetColor();
    }

    static int ListAvailable(string toolsDir)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Downloadable Tools ({Downloads.Length})\n");
        Console.ResetColor();

        int installed = 0, missing = 0;
        foreach (var dl in Downloads)
        {
            var destDir = Path.Combine(toolsDir, dl.ExtractTo);
            bool exists = Directory.Exists(destDir) &&
                (Directory.GetFiles(destDir, "*.exe", SearchOption.AllDirectories).Length > 0 ||
                 Directory.GetFiles(destDir, "*.jar", SearchOption.AllDirectories).Length > 0);

            if (exists)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("  INSTALLED ");
                installed++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  AVAILABLE ");
                missing++;
            }
            Console.ResetColor();

            Console.Write($"{dl.ToolName,-28} ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            if (dl.DirectUrl != null)
                Console.Write(dl.DirectUrl);
            else
                Console.Write($"github.com/{dl.Owner}/{dl.Repo}");
            Console.ResetColor();
            Console.WriteLine();
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  {installed} installed, {missing} available to download");
        Console.WriteLine($"  Target: {toolsDir}");
        Console.ResetColor();
        return 0;
    }

    static async Task<int> DownloadTools(string toolsDir, bool force, string? single)
    {
        Directory.CreateDirectory(toolsDir);

        var entries = Downloads.AsEnumerable();
        if (single != null)
        {
            entries = entries.Where(d =>
                d.ToolName.Contains(single, StringComparison.OrdinalIgnoreCase));
            if (!entries.Any())
            {
                Console.Error.WriteLine($"No downloadable tool matching \"{single}\"");
                Console.Error.WriteLine("Use 'lightforge setup --list' to see available tools.");
                return 1;
            }
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Lightforge", "2.0"));
        http.Timeout = TimeSpan.FromMinutes(5);

        int downloaded = 0, skipped = 0, failed = 0;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Lightforge Tool Setup\n");
        Console.ResetColor();

        foreach (var dl in entries)
        {
            var destDir = Path.Combine(toolsDir, dl.ExtractTo);
            bool alreadyExists = Directory.Exists(destDir) &&
                Directory.GetFiles(destDir, "*.*", SearchOption.AllDirectories).Length > 0;

            if (alreadyExists && !force)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  SKIP  {dl.ToolName} (already installed, use --force to re-download)");
                Console.ResetColor();
                skipped++;
                continue;
            }

            Console.Write($"  GET   {dl.ToolName,-28} ");

            try
            {
                string downloadUrl;
                string fileName;

                if (dl.DirectUrl != null)
                {
                    downloadUrl = dl.DirectUrl;
                    fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("fetching release... ");
                    Console.ResetColor();

                    var (url, name) = await FindReleaseAsset(http, dl);
                    if (url == null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("no matching release asset found");
                        Console.ResetColor();
                        failed++;
                        continue;
                    }
                    downloadUrl = url;
                    fileName = name!;
                }

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("downloading... ");
                Console.ResetColor();

                var tempFile = Path.Combine(Path.GetTempPath(), fileName);
                await DownloadFile(http, downloadUrl, tempFile);

                var fileSize = new FileInfo(tempFile).Length;

                Directory.CreateDirectory(destDir);

                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("extracting... ");
                    Console.ResetColor();
                    ExtractZip(tempFile, destDir);
                }
                else
                {
                    File.Copy(tempFile, Path.Combine(destDir, fileName), true);
                }

                File.Delete(tempFile);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"OK ({FormatSize(fileSize)})");
                Console.ResetColor();
                downloaded++;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"FAILED ({ex.Message})");
                Console.ResetColor();
                failed++;
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("\n  ");
        Console.ResetColor();

        if (downloaded > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{downloaded} downloaded");
            Console.ResetColor();
        }
        if (skipped > 0)
        {
            if (downloaded > 0) Console.Write(", ");
            Console.Write($"{skipped} skipped");
        }
        if (failed > 0)
        {
            if (downloaded > 0 || skipped > 0) Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{failed} failed");
            Console.ResetColor();
        }
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  Tools directory: {toolsDir}");
        Console.ResetColor();

        return failed > 0 ? 1 : 0;
    }

    static async Task<(string? url, string? name)> FindReleaseAsset(
        HttpClient http, DownloadEntry dl)
    {
        var apiUrl = $"https://api.github.com/repos/{dl.Owner}/{dl.Repo}/releases/latest";
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return await FindPreReleaseAsset(http, dl);
            return (null, null);
        }

        var json = await response.Content.ReadAsStringAsync();
        return MatchAsset(json, dl.AssetPattern);
    }

    static async Task<(string? url, string? name)> FindPreReleaseAsset(
        HttpClient http, DownloadEntry dl)
    {
        var apiUrl = $"https://api.github.com/repos/{dl.Owner}/{dl.Repo}/releases?per_page=5";
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return (null, null);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var release in doc.RootElement.EnumerateArray())
        {
            var result = MatchAssetFromElement(release, dl.AssetPattern);
            if (result.url != null) return result;
        }

        return (null, null);
    }

    static (string? url, string? name) MatchAsset(string releaseJson, string pattern)
    {
        using var doc = JsonDocument.Parse(releaseJson);
        return MatchAssetFromElement(doc.RootElement, pattern);
    }

    static (string? url, string? name) MatchAssetFromElement(JsonElement release, string pattern)
    {
        if (!release.TryGetProperty("assets", out var assets)) return (null, null);

        var patternLower = pattern.ToLowerInvariant();
        var parts = patternLower.Split('*', StringSplitOptions.RemoveEmptyEntries);

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString()!;
            var nameLower = name.ToLowerInvariant();

            if (!nameLower.EndsWith(".zip") && !nameLower.EndsWith(".exe") &&
                !nameLower.EndsWith(".7z") && !nameLower.EndsWith(".jar"))
                continue;

            if (nameLower.Contains("linux") || nameLower.Contains("macos") ||
                nameLower.Contains("darwin") || nameLower.Contains("osx"))
                continue;

            bool match = true;
            int pos = 0;
            foreach (var part in parts)
            {
                int idx = nameLower.IndexOf(part, pos, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) { match = false; break; }
                pos = idx + part.Length;
            }

            if (match)
            {
                var url = asset.GetProperty("browser_download_url").GetString()!;
                return (url, name);
            }
        }

        return (null, null);
    }

    static async Task DownloadFile(HttpClient http, string url, string destPath)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var file = File.Create(destPath);
        await stream.CopyToAsync(file);
    }

    static void ExtractZip(string zipPath, string destDir)
    {
        using var archive = ZipFile.OpenRead(zipPath);

        var topDirs = archive.Entries
            .Where(e => e.FullName.Contains('/'))
            .Select(e => e.FullName.Split('/')[0])
            .Distinct()
            .ToList();

        bool hasSingleRoot = topDirs.Count == 1 &&
            archive.Entries.All(e => e.FullName.StartsWith(topDirs[0] + "/") ||
                                     e.FullName == topDirs[0] + "/");

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            string relativePath = entry.FullName;
            if (hasSingleRoot)
                relativePath = relativePath[(topDirs[0].Length + 1)..];

            var destFile = Path.Combine(destDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(destFile);
            if (dir != null) Directory.CreateDirectory(dir);

            entry.ExtractToFile(destFile, true);
        }
    }

    static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
