namespace Lightforge;

static class BlpTool
{
    public static int Info(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge blp-info <file.blp|directory>");
            return 1;
        }

        var target = args[0];
        bool batch = Directory.Exists(target);

        if (batch)
        {
            var files = Directory.GetFiles(target, "*.blp", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.Error.WriteLine($"No BLP files found in: {target}");
                return 1;
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  {files.Length} BLP files in {target}\n");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  {"Name",-36} {"Size",-12} {"Dims",-14} {"Fmt",-10} Mips");
            Console.ResetColor();

            long totalSize = 0;
            foreach (var f in files.OrderBy(f => f))
            {
                var info = ReadBlpHeader(f);
                if (info == null) continue;

                totalSize += new FileInfo(f).Length;
                var name = Path.GetFileName(f);
                if (name.Length > 35) name = name[..32] + "...";

                Console.Write($"  {name,-36} ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"{FormatSize(new FileInfo(f).Length),-12} ");
                Console.ResetColor();
                Console.Write($"{info.Width}x{info.Height,-9} ");
                Console.ForegroundColor = CompressionColor(info.CompressionName);
                Console.Write($"{info.CompressionName,-10} ");
                Console.ResetColor();
                Console.WriteLine(info.MipCount);
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"\n  Total: {FormatSize(totalSize)}");
            Console.ResetColor();
            return 0;
        }

        if (!File.Exists(target))
        {
            Console.Error.WriteLine($"File not found: {target}");
            return 1;
        }

        var blp = ReadBlpHeader(target);
        if (blp == null) return 1;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {Path.GetFileName(target)}");
        Console.ResetColor();

        PrintField("Format", blp.Magic);
        PrintField("Size", $"{blp.Width} x {blp.Height}");
        PrintField("Compression", blp.CompressionName);
        PrintField("Alpha", blp.AlphaDesc);
        PrintField("Mip levels", blp.MipCount.ToString());
        PrintField("File size", FormatSize(new FileInfo(target).Length));

        long vramEstimate = EstimateVram(blp);
        if (vramEstimate > 0)
            PrintField("Est. VRAM", FormatSize(vramEstimate));

        if (blp.MipCount > 1)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  Mip chain:");
            Console.ResetColor();
            int w = (int)blp.Width, h = (int)blp.Height;
            for (int i = 0; i < blp.MipCount; i++)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"    [{i}] ");
                Console.ResetColor();
                Console.Write($"{w}x{h}");
                if (blp.MipSizes[i] > 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($"  ({FormatSize(blp.MipSizes[i])})");
                    Console.ResetColor();
                }
                Console.WriteLine();
                w = Math.Max(1, w / 2);
                h = Math.Max(1, h / 2);
            }
        }

        return 0;
    }

    record BlpHeader(string Magic, uint Width, uint Height,
        byte Compression, byte AlphaDepth, byte AlphaType, byte HasMips,
        uint[] MipOffsets, uint[] MipSizes)
    {
        public int MipCount
        {
            get
            {
                if (HasMips == 0) return 1;
                int count = 0;
                for (int i = 0; i < 16; i++)
                    if (MipSizes[i] > 0) count++;
                return Math.Max(1, count);
            }
        }

        public string CompressionName => Compression switch
        {
            0 => "JPEG",
            1 => "Palette",
            2 => AlphaType switch
            {
                0 => "DXT1",
                1 => "DXT3",
                7 => "DXT5",
                _ => $"DXT (type {AlphaType})"
            },
            3 => "Uncompressed",
            _ => $"Unknown ({Compression})"
        };

        public string AlphaDesc => AlphaDepth switch
        {
            0 => "None",
            1 => "1-bit",
            4 => "4-bit",
            8 => "8-bit",
            _ => $"{AlphaDepth}-bit"
        };
    }

    static BlpHeader? ReadBlpHeader(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            if (fs.Length < 16) return null;

            var magic = new string(br.ReadChars(4));

            if (magic == "BLP2")
            {
                uint version = br.ReadUInt32();
                byte compression = br.ReadByte();
                byte alphaDepth = br.ReadByte();
                byte alphaType = br.ReadByte();
                byte hasMips = br.ReadByte();
                uint width = br.ReadUInt32();
                uint height = br.ReadUInt32();

                var offsets = new uint[16];
                var sizes = new uint[16];
                for (int i = 0; i < 16; i++) offsets[i] = br.ReadUInt32();
                for (int i = 0; i < 16; i++) sizes[i] = br.ReadUInt32();

                return new(magic, width, height, compression, alphaDepth, alphaType, hasMips, offsets, sizes);
            }

            if (magic == "BLP1")
            {
                uint compression = br.ReadUInt32();
                uint alphaDepth = br.ReadUInt32();
                uint width = br.ReadUInt32();
                uint height = br.ReadUInt32();
                uint flags = br.ReadUInt32();
                uint hasMips = br.ReadUInt32();

                var offsets = new uint[16];
                var sizes = new uint[16];
                for (int i = 0; i < 16; i++) offsets[i] = br.ReadUInt32();
                for (int i = 0; i < 16; i++) sizes[i] = br.ReadUInt32();

                return new(magic, width, height, (byte)compression, (byte)alphaDepth, 0, (byte)hasMips, offsets, sizes);
            }

            return null;
        }
        catch { return null; }
    }

    static long EstimateVram(BlpHeader blp)
    {
        int bpp = blp.Compression switch
        {
            2 when blp.AlphaType is 0 => 4,   // DXT1: 4 bits/pixel
            2 => 8,                             // DXT3/DXT5: 8 bits/pixel
            1 => 8,                             // Palette: 8 bits/pixel index
            3 => 32,                            // Uncompressed ARGB
            _ => 0
        };

        if (bpp == 0) return 0;

        long total = 0;
        int w = (int)blp.Width, h = (int)blp.Height;
        for (int i = 0; i < blp.MipCount; i++)
        {
            total += (long)w * h * bpp / 8;
            w = Math.Max(1, w / 2);
            h = Math.Max(1, h / 2);
        }
        return total;
    }

    static ConsoleColor CompressionColor(string name) => name switch
    {
        "DXT1" => ConsoleColor.Green,
        "DXT3" or "DXT5" => ConsoleColor.Cyan,
        "Palette" => ConsoleColor.Blue,
        "JPEG" => ConsoleColor.Yellow,
        "Uncompressed" => ConsoleColor.Red,
        _ => ConsoleColor.Gray
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
