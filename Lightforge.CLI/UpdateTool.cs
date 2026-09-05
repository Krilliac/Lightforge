using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Lightforge;

static class UpdateTool
{
    record ToolVersion(string Name, string Owner, string Repo, string InstalledDir);

    static readonly ToolVersion[] Tracked =
    [
        new("Noggit3",         "wowdev",       "noggit3",           "noggit3"),
        new("WDBXEditor",      "WowDevTools",  "WDBXEditor",        "WDBXEditor"),
        new("WDBXEditor2",     "MaxtorCoder",  "WDBXEditor2",       "WDBXEditor2"),
        new("BLPConverter",    "Kanma",        "BLPConverter",       "BLPConverter"),
        new("CASCExplorer",    "WoW-Tools",    "CASCExplorer",      "CASCExplorer"),
        new("CASCHost",        "WowDevTools",  "CASCHost",          "CASCHost"),
        new("wow.export",      "Kruithne",     "wow.export",        "wowexport"),
        new("WowPacketParser", "TrinityCore",  "WowPacketParser",   "WowPacketParser"),
        new("MultiConverter",  "MaxtorCoder",  "MultiConverter",    "MultiConverter"),
        new("Keira3",          "azerothcore",  "Keira3",            "Keira3"),
        new("WMVx",            "Frostshake",   "WMVx",              "WMVx"),
        new("mpqcli",          "TheGrayDot",   "mpqcli",            "mpqcli"),
        new("WoW Database Editor","BAndysc",   "WoWDatabaseEditor", "WDE"),
        new("M2Mod",           "M2Mod",        "m2mod",             "M2Mod"),
        new("Spell Editor",    "stoneharry",   "WoW-Spell-Editor",  "SpellEditor"),
        new("TrinityCreator",  "NotCoffee418", "TrinityCreator",    "TrinityCreator"),
        new("wow-patcher",     "wowemulation-dev","wow-patcher",    "wowpatcher"),
        new("Arctium Launcher","Arctium",      "WoW-Launcher",      "ArctiumLauncher"),
        new("VanillaFixes",    "hannesmann",   "vanillafixes",      "VanillaFixes"),
        new("WoWHeightGen",    "CucFlavius",   "WoWHeightGen",      "WoWHeightGen"),
    ];

    public static int Run(string[] args)
    {
        string? toolsDir = null;
        string? single = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--path" or "-p" when i + 1 < args.Length:
                    toolsDir = args[++i];
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
        return CheckUpdates(toolsDir, single).GetAwaiter().GetResult();
    }

    static void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Tool Update Checker\n");
        Console.ResetColor();
        Console.WriteLine("Usage: lightforge check-updates [options]\n");
        Console.WriteLine("Compares installed tool versions against latest GitHub releases.\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --path, -p <dir>    Toolset Binaries directory (default: next to exe)");
        Console.WriteLine("  --tool, -t <name>   Check only this tool");
    }

    static async Task<int> CheckUpdates(string toolsDir, string? single)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Lightforge", "2.0"));
        http.Timeout = TimeSpan.FromSeconds(15);

        var entries = Tracked.AsEnumerable();
        if (single != null)
        {
            entries = entries.Where(t =>
                t.Name.Contains(single, StringComparison.OrdinalIgnoreCase));
            if (!entries.Any())
            {
                Console.Error.WriteLine($"No tracked tool matching \"{single}\"");
                return 1;
            }
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Tool Update Check\n");
        Console.ResetColor();

        int upToDate = 0, updates = 0, notInstalled = 0, errors = 0;

        foreach (var tool in entries)
        {
            var destDir = Path.Combine(toolsDir, tool.InstalledDir);
            bool installed = Directory.Exists(destDir) &&
                Directory.GetFiles(destDir, "*.*", SearchOption.AllDirectories).Length > 0;

            if (!installed)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  ---     {tool.Name,-28} not installed");
                Console.ResetColor();
                notInstalled++;
                continue;
            }

            var localDate = GetLocalDate(destDir);

            try
            {
                var (tag, publishDate) = await GetLatestRelease(http, tool.Owner, tool.Repo);
                if (tag == null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"  ???     {tool.Name,-28} ");
                    Console.WriteLine("no releases found");
                    Console.ResetColor();
                    errors++;
                    continue;
                }

                bool hasUpdate = publishDate > localDate.AddHours(1);

                if (hasUpdate)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"  UPDATE  {tool.Name,-28} ");
                    Console.ResetColor();
                    Console.Write($"installed: {localDate:yyyy-MM-dd}  ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"latest: {tag}");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($" ({publishDate:yyyy-MM-dd})");
                    Console.ResetColor();
                    Console.WriteLine();
                    updates++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"  OK      {tool.Name,-28} ");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"{tag}");
                    Console.ResetColor();
                    Console.WriteLine();
                    upToDate++;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"  ERR     {tool.Name,-28} ");
                Console.ResetColor();
                Console.WriteLine(ex.Message);
                errors++;
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  ");
        Console.ResetColor();

        if (updates > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{updates} update(s) available");
            Console.ResetColor();
        }
        if (upToDate > 0)
        {
            if (updates > 0) Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{upToDate} up to date");
            Console.ResetColor();
        }
        if (notInstalled > 0)
        {
            if (updates > 0 || upToDate > 0) Console.Write(", ");
            Console.Write($"{notInstalled} not installed");
        }
        if (errors > 0)
        {
            Console.Write(", ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"{errors} error(s)");
            Console.ResetColor();
        }
        Console.WriteLine();

        if (updates > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Run 'lightforge setup --force -t <name>' to update a tool.");
            Console.ResetColor();
        }

        return 0;
    }

    static DateTime GetLocalDate(string dir)
    {
        var newest = DateTime.MinValue;
        foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
        {
            var written = File.GetLastWriteTimeUtc(file);
            if (written > newest) newest = written;
        }
        return newest;
    }

    static async Task<(string? tag, DateTime published)> GetLatestRelease(
        HttpClient http, string owner, string repo)
    {
        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var response = await http.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            var listUrl = $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=1";
            var listReq = new HttpRequestMessage(HttpMethod.Get, listUrl);
            listReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            var listResp = await http.SendAsync(listReq);
            if (!listResp.IsSuccessStatusCode) return (null, DateTime.MinValue);

            var listJson = await listResp.Content.ReadAsStringAsync();
            using var listDoc = JsonDocument.Parse(listJson);
            var arr = listDoc.RootElement;
            if (arr.GetArrayLength() == 0) return (null, DateTime.MinValue);

            var first = arr[0];
            var t = first.GetProperty("tag_name").GetString();
            var p = DateTime.Parse(first.GetProperty("published_at").GetString()!).ToUniversalTime();
            return (t, p);
        }

        if (!response.IsSuccessStatusCode) return (null, DateTime.MinValue);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var tag = doc.RootElement.GetProperty("tag_name").GetString();
        var published = DateTime.Parse(
            doc.RootElement.GetProperty("published_at").GetString()!).ToUniversalTime();
        return (tag, published);
    }
}
