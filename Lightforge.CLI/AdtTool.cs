using System.Text;

namespace Lightforge;

static class AdtTool
{
    public static int Info(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge adt-info <file.adt>");
            return 1;
        }

        var path = args[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 1;
        }

        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (fs.Length < 12)
        {
            Console.Error.WriteLine("File too small to be a valid ADT");
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {Path.GetFileName(path)}");
        Console.ResetColor();
        PrintField("File size", FormatSize(fs.Length));

        var chunks = new List<ChunkInfo>();
        var textures = new List<string>();
        var models = new List<string>();
        var wmos = new List<string>();
        int doodadCount = 0;
        int wmoCount = 0;
        int mcnkCount = 0;
        uint version = 0;

        while (fs.Position + 8 <= fs.Length)
        {
            long chunkStart = fs.Position;
            var magicBytes = br.ReadBytes(4);
            if (magicBytes.Length < 4) break;

            Array.Reverse(magicBytes);
            string magic = Encoding.ASCII.GetString(magicBytes);
            uint size = br.ReadUInt32();

            long dataPos = fs.Position;
            long nextChunk = dataPos + size;
            if (nextChunk > fs.Length) break;

            chunks.Add(new(magic, size, chunkStart));

            switch (magic)
            {
                case "MVER":
                    if (size >= 4) version = br.ReadUInt32();
                    break;

                case "MTEX":
                    textures.AddRange(ReadNullTerminatedStrings(br, size));
                    break;

                case "MMDX":
                    models.AddRange(ReadNullTerminatedStrings(br, size));
                    break;

                case "MWMO":
                    wmos.AddRange(ReadNullTerminatedStrings(br, size));
                    break;

                case "MDDF":
                    doodadCount = (int)(size / 36);
                    break;

                case "MODF":
                    wmoCount = (int)(size / 64);
                    break;

                case "MCNK":
                    mcnkCount++;
                    break;
            }

            fs.Seek(nextChunk, SeekOrigin.Begin);
        }

        if (version > 0)
            PrintField("Version", $"MVER {version}");
        PrintField("Chunks", $"{chunks.Count} total ({mcnkCount} MCNK terrain)");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n  Chunk layout:");
        Console.ResetColor();

        var grouped = chunks.Where(c => c.Magic != "MCNK").ToList();
        foreach (var c in grouped)
        {
            Console.ForegroundColor = ChunkColor(c.Magic);
            Console.Write($"    {c.Magic}");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {FormatSize(c.Size),-12}");
            Console.ResetColor();
            Console.WriteLine(ChunkDesc(c.Magic, c.Size));
        }

        if (mcnkCount > 0)
        {
            Console.ForegroundColor = ChunkColor("MCNK");
            Console.Write($"    MCNK");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  x{mcnkCount,-10}");
            Console.ResetColor();
            Console.WriteLine("Terrain chunks");
        }

        if (textures.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n  Textures ({textures.Count}):");
            Console.ResetColor();
            foreach (var t in textures.Take(20))
                Console.WriteLine($"    {t}");
            if (textures.Count > 20)
                Console.WriteLine($"    ... +{textures.Count - 20} more");
        }

        if (models.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n  Models ({models.Count}):");
            Console.ResetColor();
            foreach (var m in models.Take(15))
                Console.WriteLine($"    {m}");
            if (models.Count > 15)
                Console.WriteLine($"    ... +{models.Count - 15} more");
        }

        if (wmos.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n  WMOs ({wmos.Count}):");
            Console.ResetColor();
            foreach (var w in wmos.Take(10))
                Console.WriteLine($"    {w}");
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"\n  Placements: ");
        Console.ResetColor();
        Console.WriteLine($"{doodadCount} doodads, {wmoCount} WMOs");

        return 0;
    }

    record ChunkInfo(string Magic, uint Size, long Offset);

    static List<string> ReadNullTerminatedStrings(BinaryReader br, uint blockSize)
    {
        var result = new List<string>();
        var bytes = br.ReadBytes((int)Math.Min(blockSize, 1024 * 64));
        int start = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] == 0 && i > start)
            {
                result.Add(Encoding.ASCII.GetString(bytes, start, i - start));
                start = i + 1;
            }
            else if (bytes[i] == 0)
            {
                start = i + 1;
            }
        }
        return result;
    }

    static ConsoleColor ChunkColor(string magic) => magic switch
    {
        "MVER" or "MHDR" => ConsoleColor.Yellow,
        "MCIN" => ConsoleColor.DarkGray,
        "MTEX" => ConsoleColor.Green,
        "MMDX" or "MMID" => ConsoleColor.Cyan,
        "MWMO" or "MWID" => ConsoleColor.Magenta,
        "MDDF" or "MODF" => ConsoleColor.Blue,
        "MH2O" or "MFBO" => ConsoleColor.DarkCyan,
        "MCNK" => ConsoleColor.White,
        _ => ConsoleColor.Gray
    };

    static string ChunkDesc(string magic, uint size) => magic switch
    {
        "MVER" => "Version",
        "MHDR" => "Header offsets",
        "MCIN" => $"Chunk index ({size / 16} entries)",
        "MTEX" => "Texture filenames",
        "MMDX" => "Model filenames",
        "MMID" => $"Model offsets ({size / 4} entries)",
        "MWMO" => "WMO filenames",
        "MWID" => $"WMO offsets ({size / 4} entries)",
        "MDDF" => $"Doodad placements ({size / 36})",
        "MODF" => $"WMO placements ({size / 64})",
        "MH2O" => "Water data",
        "MFBO" => "Flight bounds",
        "MTFX" => "Texture effects",
        _ => ""
    };

    static void PrintField(string label, string value)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {label + ":",-16}");
        Console.ResetColor();
        Console.WriteLine(value);
    }

    static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB"
    };
}
