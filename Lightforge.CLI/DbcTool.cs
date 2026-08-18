namespace Lightforge;

static class DbcTool
{
    public static int Info(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: lightforge dbc-info <file.dbc>");
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

        if (fs.Length < 20)
        {
            Console.Error.WriteLine("File too small to be a valid DBC/DB2");
            return 1;
        }

        var magic = new string(br.ReadChars(4));
        var header = ParseHeader(magic, br);
        if (header == null)
        {
            Console.Error.WriteLine($"Unknown format: {magic}");
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  {Path.GetFileName(path)}");
        Console.ResetColor();

        PrintField("Format", header.Format);
        PrintField("Records", header.RecordCount.ToString("N0"));
        PrintField("Fields", header.FieldCount.ToString());
        PrintField("Record size", $"{header.RecordSize} bytes");
        PrintField("String block", $"{header.StringBlockSize:N0} bytes");
        PrintField("File size", FormatSize(fs.Length));

        if (header.MinId > 0)
            PrintField("ID range", $"{header.MinId} - {header.MaxId}");

        long dataSize = (long)header.RecordCount * header.RecordSize;
        long stringStart = header.HeaderSize + dataSize;

        if (header.StringBlockSize > 0 && stringStart + header.StringBlockSize <= fs.Length)
        {
            fs.Seek(stringStart, SeekOrigin.Begin);
            var block = br.ReadBytes((int)Math.Min(header.StringBlockSize, 1024 * 64));
            int strCount = 0;
            for (int i = 0; i < block.Length; i++)
                if (block[i] == 0 && i > 0 && block[i - 1] != 0) strCount++;

            PrintField("Strings", header.StringBlockSize <= 65536
                ? strCount.ToString("N0")
                : $"~{strCount}+ (sampled first 64 KB)");
        }

        int stringFields = CountStringFields(fs, header);
        if (stringFields >= 0)
            PrintField("String fields", stringFields.ToString());

        return 0;
    }

    public static int Diff(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: lightforge dbc-diff <file1.dbc> <file2.dbc>");
            return 1;
        }

        if (!File.Exists(args[0])) { Console.Error.WriteLine($"File not found: {args[0]}"); return 1; }
        if (!File.Exists(args[1])) { Console.Error.WriteLine($"File not found: {args[1]}"); return 1; }

        var (headA, recordsA) = LoadDbc(args[0]);
        var (headB, recordsB) = LoadDbc(args[1]);

        if (headA == null || headB == null) return 1;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Comparing DBC files\n");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  A: ");
        Console.ResetColor();
        Console.WriteLine($"{Path.GetFileName(args[0])}  ({headA.RecordCount:N0} records, {headA.FieldCount} fields)");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  B: ");
        Console.ResetColor();
        Console.WriteLine($"{Path.GetFileName(args[1])}  ({headB.RecordCount:N0} records, {headB.FieldCount} fields)");
        Console.WriteLine();

        if (headA.FieldCount != headB.FieldCount || headA.RecordSize != headB.RecordSize)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  Schema mismatch - files have different field counts or record sizes.");
            Console.ResetColor();
            Console.WriteLine($"  A: {headA.FieldCount} fields, {headA.RecordSize} bytes/record");
            Console.WriteLine($"  B: {headB.FieldCount} fields, {headB.RecordSize} bytes/record");
            return 1;
        }

        var dictA = new Dictionary<uint, byte[]>();
        var dictB = new Dictionary<uint, byte[]>();

        int fieldsPerRecord = (int)(headA.RecordSize / 4);
        foreach (var rec in recordsA)
        {
            uint id = BitConverter.ToUInt32(rec, 0);
            dictA[id] = rec;
        }
        foreach (var rec in recordsB)
        {
            uint id = BitConverter.ToUInt32(rec, 0);
            dictB[id] = rec;
        }

        var added = dictB.Keys.Except(dictA.Keys).OrderBy(x => x).ToList();
        var removed = dictA.Keys.Except(dictB.Keys).OrderBy(x => x).ToList();
        var common = dictA.Keys.Intersect(dictB.Keys).OrderBy(x => x).ToList();

        var modified = new List<(uint id, List<int> changedFields)>();
        foreach (var id in common)
        {
            var a = dictA[id];
            var b = dictB[id];
            var changes = new List<int>();
            for (int f = 1; f < fieldsPerRecord; f++)
            {
                uint va = BitConverter.ToUInt32(a, f * 4);
                uint vb = BitConverter.ToUInt32(b, f * 4);
                if (va != vb) changes.Add(f);
            }
            if (changes.Count > 0)
                modified.Add((id, changes));
        }

        bool verbose = args.Any(a => a == "--verbose" || a == "-v");
        int maxShow = 25;

        if (added.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  +{added.Count} records added");
            Console.ResetColor();
            foreach (var id in added.Take(maxShow))
                Console.WriteLine($"    ID {id}");
            if (added.Count > maxShow)
                Console.WriteLine($"    ... and {added.Count - maxShow} more");
            Console.WriteLine();
        }

        if (removed.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  -{removed.Count} records removed");
            Console.ResetColor();
            foreach (var id in removed.Take(maxShow))
                Console.WriteLine($"    ID {id}");
            if (removed.Count > maxShow)
                Console.WriteLine($"    ... and {removed.Count - maxShow} more");
            Console.WriteLine();
        }

        if (modified.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  ~{modified.Count} records modified");
            Console.ResetColor();
            foreach (var (id, changes) in modified.Take(maxShow))
            {
                if (verbose)
                {
                    Console.Write($"    ID {id}: ");
                    var parts = new List<string>();
                    foreach (int f in changes.Take(8))
                    {
                        uint va = BitConverter.ToUInt32(dictA[id], f * 4);
                        uint vb = BitConverter.ToUInt32(dictB[id], f * 4);
                        parts.Add($"[{f}] {va}→{vb}");
                    }
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(string.Join(", ", parts));
                    if (changes.Count > 8) Console.Write($" +{changes.Count - 8} more");
                    Console.ResetColor();
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine($"    ID {id}: {changes.Count} field{(changes.Count > 1 ? "s" : "")} changed");
                }
            }
            if (modified.Count > maxShow)
                Console.WriteLine($"    ... and {modified.Count - maxShow} more");
            Console.WriteLine();
        }

        if (added.Count == 0 && removed.Count == 0 && modified.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  Files are identical.");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  Summary: +{added.Count} added, ~{modified.Count} modified, -{removed.Count} removed");
            if (!verbose && modified.Count > 0)
                Console.WriteLine("  Use --verbose to see field-level changes");
            Console.ResetColor();
        }

        return 0;
    }

    record DbcHeader(string Format, uint RecordCount, uint FieldCount,
        uint RecordSize, uint StringBlockSize, uint MinId, uint MaxId, int HeaderSize);

    static DbcHeader? ParseHeader(string magic, BinaryReader br)
    {
        switch (magic)
        {
            case "WDBC":
            {
                uint rc = br.ReadUInt32(), fc = br.ReadUInt32();
                uint rs = br.ReadUInt32(), sb = br.ReadUInt32();
                return new("WDBC", rc, fc, rs, sb, 0, 0, 20);
            }
            case "WDB2":
            {
                uint rc = br.ReadUInt32(), fc = br.ReadUInt32();
                uint rs = br.ReadUInt32(), sb = br.ReadUInt32();
                uint hash = br.ReadUInt32(), build = br.ReadUInt32();
                uint stamp = br.ReadUInt32();
                uint minId = br.ReadUInt32(), maxId = br.ReadUInt32();
                uint locale = br.ReadUInt32(), copySize = br.ReadUInt32();
                return new("WDB2", rc, fc, rs, sb, minId, maxId, 48);
            }
            case "WDB5":
            {
                uint rc = br.ReadUInt32(), fc = br.ReadUInt32();
                uint rs = br.ReadUInt32(), sb = br.ReadUInt32();
                uint hash = br.ReadUInt32(), build = br.ReadUInt32();
                uint minId = br.ReadUInt32(), maxId = br.ReadUInt32();
                uint locale = br.ReadUInt32(), copySize = br.ReadUInt32();
                ushort flags = br.ReadUInt16(), idCol = br.ReadUInt16();
                return new("WDB5", rc, fc, rs, sb, minId, maxId, 52);
            }
            case "WDB6":
            {
                uint rc = br.ReadUInt32(), fc = br.ReadUInt32();
                uint rs = br.ReadUInt32(), sb = br.ReadUInt32();
                uint hash = br.ReadUInt32(), build = br.ReadUInt32();
                uint minId = br.ReadUInt32(), maxId = br.ReadUInt32();
                uint locale = br.ReadUInt32(), copySize = br.ReadUInt32();
                ushort flags = br.ReadUInt16(), idCol = br.ReadUInt16();
                uint totalFc = br.ReadUInt32();
                return new("WDB6", rc, fc, rs, sb, minId, maxId, 56);
            }
            default:
                return null;
        }
    }

    static int CountStringFields(FileStream fs, DbcHeader h)
    {
        if (h.StringBlockSize <= 1 || h.RecordCount == 0) return 0;

        int fieldsPerRecord = (int)(h.RecordSize / 4);
        long dataStart = h.HeaderSize;
        long stringStart = dataStart + (long)h.RecordCount * h.RecordSize;

        fs.Seek(dataStart, SeekOrigin.Begin);
        using var br = new BinaryReader(fs, System.Text.Encoding.UTF8, true);

        int sampleSize = (int)Math.Min(h.RecordCount, 100);
        var couldBeString = new int[fieldsPerRecord];

        for (int r = 0; r < sampleSize; r++)
        {
            for (int f = 0; f < fieldsPerRecord; f++)
            {
                uint val = br.ReadUInt32();
                if (val > 0 && val < h.StringBlockSize)
                    couldBeString[f]++;
            }
        }

        int count = 0;
        for (int f = 0; f < fieldsPerRecord; f++)
            if (couldBeString[f] >= sampleSize * 0.8) count++;

        return count;
    }

    static (DbcHeader? header, List<byte[]> records) LoadDbc(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (fs.Length < 20)
        {
            Console.Error.WriteLine($"File too small: {path}");
            return (null, []);
        }

        var magic = new string(br.ReadChars(4));
        var header = ParseHeader(magic, br);
        if (header == null)
        {
            Console.Error.WriteLine($"Unknown format ({magic}): {path}");
            return (null, []);
        }

        fs.Seek(header.HeaderSize, SeekOrigin.Begin);
        var records = new List<byte[]>((int)header.RecordCount);
        for (uint i = 0; i < header.RecordCount; i++)
            records.Add(br.ReadBytes((int)header.RecordSize));

        return (header, records);
    }

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
