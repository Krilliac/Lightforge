namespace Lightforge;

static class ListfileTool
{
    public static int Search(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge listfile <pattern> [listfile.csv]");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Search the community listfile for WoW client file paths.");
            Console.Error.WriteLine("Pattern supports wildcards: * (any chars), ? (single char)");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Examples:");
            Console.Error.WriteLine("  lightforge listfile stormwind");
            Console.Error.WriteLine("  lightforge listfile *.blp --ext blp");
            Console.Error.WriteLine("  lightforge listfile \"world/maps/azeroth/*\" listfile.csv");
            return 1;
        }

        string pattern = args[0].ToLowerInvariant();
        string? listfilePath = null;
        string? extFilter = null;
        int maxResults = 100;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--ext" when i + 1 < args.Length:
                    extFilter = args[++i].TrimStart('.').ToLowerInvariant();
                    break;
                case "--limit" or "-n" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out int n)) maxResults = n;
                    break;
                case "--count":
                    maxResults = 0;
                    break;
                default:
                    if (!args[i].StartsWith("-"))
                        listfilePath = args[i];
                    break;
            }
        }

        if (listfilePath == null)
        {
            listfilePath = FindListfile();
            if (listfilePath == null)
            {
                Console.Error.WriteLine("No listfile found. Provide the path as an argument:");
                Console.Error.WriteLine("  lightforge listfile <pattern> community-listfile.csv");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Download from: https://github.com/wowdev/wow-listfile");
                return 1;
            }
        }

        if (!File.Exists(listfilePath))
        {
            Console.Error.WriteLine($"Listfile not found: {listfilePath}");
            return 1;
        }

        bool isGlob = pattern.Contains('*') || pattern.Contains('?');
        int matchCount = 0;
        int totalLines = 0;
        var results = new List<(string id, string path)>();

        using var reader = new StreamReader(listfilePath);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            totalLines++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            string id, filePath;
            int sep = line.IndexOfAny([';', ',']);
            if (sep > 0)
            {
                id = line[..sep];
                filePath = line[(sep + 1)..];
            }
            else
            {
                id = "";
                filePath = line;
            }

            var pathLower = filePath.ToLowerInvariant();

            if (extFilter != null && !pathLower.EndsWith("." + extFilter))
                continue;

            bool match;
            if (isGlob)
                match = GlobMatch(pattern, pathLower);
            else
                match = pathLower.Contains(pattern);

            if (match)
            {
                matchCount++;
                if (maxResults == 0) continue;
                if (results.Count < maxResults)
                    results.Add((id, filePath));
            }
        }

        if (matchCount == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"No matches for \"{pattern}\" in {totalLines:N0} entries");
            Console.ResetColor();
            return 0;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {matchCount:N0} matches for \"{pattern}\"\n");
        Console.ResetColor();

        if (maxResults != 0)
        {
            bool hasIds = results.Any(r => r.id.Length > 0);
            foreach (var (id, path) in results)
            {
                if (hasIds)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"  {id,10}  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write("  ");
                }

                var ext = Path.GetExtension(path).ToLowerInvariant();
                Console.ForegroundColor = ExtColor(ext);
                Console.WriteLine(path);
                Console.ResetColor();
            }

            if (matchCount > results.Count)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"\n  ... {matchCount - results.Count:N0} more (use --limit N or --count)");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  (count-only mode, use --limit N to show results)");
            Console.ResetColor();
        }

        var extGroups = results.GroupBy(r => Path.GetExtension(r.path).ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(8);

        if (results.Count > 5)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("\n  By type: ");
            Console.ResetColor();
            Console.WriteLine(string.Join(", ", extGroups.Select(g => $"{g.Key} ({g.Count()})")));
        }

        return 0;
    }

    static bool GlobMatch(string pattern, string text)
    {
        int pi = 0, ti = 0;
        int starPi = -1, starTi = -1;

        while (ti < text.Length)
        {
            if (pi < pattern.Length && (pattern[pi] == '?' || pattern[pi] == text[ti]))
            {
                pi++; ti++;
            }
            else if (pi < pattern.Length && pattern[pi] == '*')
            {
                starPi = pi++;
                starTi = ti;
            }
            else if (starPi >= 0)
            {
                pi = starPi + 1;
                ti = ++starTi;
            }
            else
            {
                return false;
            }
        }

        while (pi < pattern.Length && pattern[pi] == '*') pi++;
        return pi == pattern.Length;
    }

    static string? FindListfile()
    {
        var candidates = new[]
        {
            "listfile.csv",
            "community-listfile.csv",
            Path.Combine(AppContext.BaseDirectory, "listfile.csv"),
            Path.Combine(AppContext.BaseDirectory, "community-listfile.csv"),
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    static ConsoleColor ExtColor(string ext) => ext switch
    {
        ".blp" => ConsoleColor.Green,
        ".m2" => ConsoleColor.Cyan,
        ".wmo" => ConsoleColor.Magenta,
        ".adt" => ConsoleColor.Yellow,
        ".dbc" or ".db2" => ConsoleColor.Blue,
        ".wdt" => ConsoleColor.DarkYellow,
        ".skin" => ConsoleColor.DarkCyan,
        ".anim" => ConsoleColor.DarkGreen,
        ".ogg" or ".mp3" or ".wav" => ConsoleColor.Red,
        _ => ConsoleColor.Gray
    };
}
