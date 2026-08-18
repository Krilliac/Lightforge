namespace Lightforge;

public enum WowVersion
{
    Vanilla,   // 1.12.1
    TBC,       // 2.4.3
    WotLK,     // 3.3.5a
    Cata,      // 4.3.4
    MoP,       // 5.4.8
    WoD,       // 6.2.4
    Legion,    // 7.3.5
    BfA,       // 8.3.7
    All        // show everything
}

public static class WowVersionInfo
{
    public static readonly (WowVersion version, string display, string build)[] Versions =
    [
        (WowVersion.All,     "All Versions",    ""),
        (WowVersion.Vanilla, "Vanilla 1.12.1",  "5875"),
        (WowVersion.TBC,     "TBC 2.4.3",       "8606"),
        (WowVersion.WotLK,   "WotLK 3.3.5a",    "12340"),
        (WowVersion.Cata,    "Cata 4.3.4",       "15595"),
        (WowVersion.MoP,     "MoP 5.4.8",        "18414"),
        (WowVersion.WoD,     "WoD 6.2.4",        "21742"),
        (WowVersion.Legion,  "Legion 7.3.5",     "26972"),
        (WowVersion.BfA,     "BfA 8.3.7",        "35662"),
    ];

    public static string DisplayName(WowVersion v) =>
        Versions.First(x => x.version == v).display;

    public static WowVersion FromDisplay(string display) =>
        Versions.FirstOrDefault(x => x.display == display).version;

    public static bool UsesMpq(WowVersion v) =>
        v is WowVersion.Vanilla or WowVersion.TBC or WowVersion.WotLK or WowVersion.Cata or WowVersion.MoP;

    public static bool UsesCasc(WowVersion v) =>
        v is WowVersion.WoD or WowVersion.Legion or WowVersion.BfA;
}

public record ToolEntry(
    string Name,
    string Description,
    string RelativePath,
    string Category,
    WowVersion[] Compatible,
    string Source = "",
    string Status = "")
{
    public bool IsCompatible(WowVersion selected) =>
        selected == WowVersion.All || Compatible.Contains(selected);
}

public static class WowToolRegistry
{
    private static readonly WowVersion[] AllMpq =
        [WowVersion.Vanilla, WowVersion.TBC, WowVersion.WotLK, WowVersion.Cata, WowVersion.MoP];

    private static readonly WowVersion[] AllCasc =
        [WowVersion.WoD, WowVersion.Legion, WowVersion.BfA];

    private static readonly WowVersion[] AllVersions =
        [WowVersion.Vanilla, WowVersion.TBC, WowVersion.WotLK, WowVersion.Cata,
         WowVersion.MoP, WowVersion.WoD, WowVersion.Legion, WowVersion.BfA];

    private static readonly WowVersion[] ClassicTrio =
        [WowVersion.Vanilla, WowVersion.TBC, WowVersion.WotLK];

    private static readonly WowVersion[] WotlkOnly = [WowVersion.WotLK];
    private static readonly WowVersion[] VanillaOnly = [WowVersion.Vanilla];
    private static readonly WowVersion[] WotlkTbc = [WowVersion.TBC, WowVersion.WotLK];
    private static readonly WowVersion[] WotlkCata = [WowVersion.WotLK, WowVersion.Cata];
    private static readonly WowVersion[] CataPlus =
        [WowVersion.Cata, WowVersion.MoP, WowVersion.WoD, WowVersion.Legion, WowVersion.BfA];

    public static ToolEntry[] GetAllTools() =>
    [
        // ══════════════════════════════════════════════════
        //  WORLD EDITING
        // ══════════════════════════════════════════════════
        new("Noggit3",
            "3D map terrain and doodad editor",
            @"noggit3\noggit",
            "WORLD EDITING", WotlkOnly,
            "https://github.com/wowdev/noggit3", "Active"),

        new("Noggit RED",
            "Enhanced Noggit fork with undo, asset browser",
            @"NoggitRed\noggit",
            "WORLD EDITING", WotlkOnly,
            "https://gitlab.com/varenroth/noggit-red", "Active"),

        new("WoW Blender Studio",
            "Blender addon for WMO/M2/ADT editing",
            @"BlenderStudio\launch.cmd",
            "WORLD EDITING", ClassicTrio,
            "https://gitlab.com/skarnproject/blender-wow-studio", "Active"),

        new("ADT Creator",
            "Generate blank ADT and WDT files for new maps",
            @"adtcreator\adtcreator",
            "WORLD EDITING", ClassicTrio,
            "https://github.com/tswow/adt-creator", "Active"),

        new("AdtTools",
            "Programmatic ADT file manipulation framework",
            @"AdtTools\AdtTools",
            "WORLD EDITING", ClassicTrio,
            "https://github.com/kelno/AdtTools", "Maintained"),

        new("MapUpconverter",
            "Convert 3.3.5 ADTs to modern format (7.x/8.x/9.x)",
            @"MapUpconverter\MapUpconverter",
            "WORLD EDITING", [WowVersion.WotLK, WowVersion.Legion, WowVersion.BfA],
            "https://github.com/ModernWoWTools/MapUpconverter", "Active"),

        new("Neo",
            "Map viewer and editor for modern WoW formats",
            @"Neo\Neo",
            "WORLD EDITING", [WowVersion.WotLK, WowVersion.WoD],
            "https://github.com/WowDevTools/Neo", "Maintained"),

        // ══════════════════════════════════════════════════
        //  DATA EDITING
        // ══════════════════════════════════════════════════
        new("WDBXEditor",
            "Edit all DBC/DB2/WDB/ADB client database files",
            @"WDBXEditor\WDBXEditor",
            "DATA EDITING", AllVersions,
            "https://github.com/WowDevTools/WDBXEditor", "Active"),

        new("WDBXEditor2",
            "Modern DB2 editor for newer WoW versions",
            @"WDBXEditor2\WDBXEditor2",
            "DATA EDITING", CataPlus,
            "https://github.com/MaxtorCoder/WDBXEditor2", "Active"),

        new("Spell Editor",
            "Edit Spell.dbc with visual interface",
            @"SpellEditor\Spell Editor V2",
            "DATA EDITING", ClassicTrio,
            "https://github.com/stoneharry/WoW-Spell-Editor", "Active"),

        new("SpellWork",
            "Spell info viewer and inspector",
            @"SpellWork\SpellWork",
            "DATA EDITING", AllVersions,
            "https://github.com/TrinityCore/SpellWork", "Active"),

        new("WoW Database Editor",
            "Full IDE for SmartAI, DB editing, 3D viewer",
            @"WDE\WoWDatabaseEditor",
            "DATA EDITING", [WowVersion.WotLK, WowVersion.Cata],
            "https://github.com/BAndysc/WoWDatabaseEditor", "Active"),

        new("Keira3",
            "AzerothCore database editor (no SQL needed)",
            @"Keira3\keira3",
            "DATA EDITING", WotlkOnly,
            "https://github.com/azerothcore/Keira3", "Active"),

        new("TrinityCreator",
            "GUI creator for items, creatures, quests",
            @"TrinityCreator\TrinityCreator",
            "DATA EDITING", WotlkOnly,
            "https://github.com/NotCoffee418/TrinityCreator", "Maintained"),

        new("AoWoW",
            "Wowhead-style web database viewer",
            @"aowow\launch.cmd",
            "DATA EDITING", WotlkOnly,
            "https://github.com/Sarjuuk/aowow", "Active"),

        new("SAI-Editor",
            "SmartAI event script editor for TrinityCore",
            @"SAI-Editor\SAI-Editor",
            "DATA EDITING", WotlkCata,
            "https://github.com/jasper-rietrae2/SAI-Editor", "Maintained"),

        // ══════════════════════════════════════════════════
        //  MODEL TOOLS
        // ══════════════════════════════════════════════════
        new("M2Mod",
            "Edit M2 model meshes via Blender M2I pipeline",
            @"M2Mod\m2mod",
            "MODEL TOOLS", ClassicTrio,
            "https://github.com/M2Mod/m2mod", "Maintained"),

        new("MultiConverter",
            "Retroport models from modern WoW to older versions",
            @"MultiConverter\MultiConverter",
            "MODEL TOOLS", AllVersions,
            "https://github.com/MaxtorCoder/MultiConverter", "Archived"),

        new("BLPConverter",
            "Convert BLP textures to/from PNG and TGA",
            @"BLPConverter\BLPConverter",
            "MODEL TOOLS", AllVersions,
            "https://github.com/Kanma/BLPConverter", "Stable"),

        new("BLP Lab",
            "GUI BLP texture editor and converter",
            @"BLPLab\BLP Lab",
            "MODEL TOOLS", AllVersions,
            "", "Active"),

        new("jM2converter",
            "Universal M2 format version converter (Java)",
            @"jM2converter\jM2converter",
            "MODEL TOOLS", AllVersions,
            "https://github.com/WowDevs/jM2converter", "Stale"),

        new("Map Asset Parser",
            "Extract asset list from map for minimal patching",
            @"MapAssetParser\WoW-Map-Asset-Parser",
            "MODEL TOOLS", ClassicTrio,
            "https://github.com/stoneharry/WoW-Map-Asset-Parser", "Maintained"),

        // ══════════════════════════════════════════════════
        //  VIEWERS
        // ══════════════════════════════════════════════════
        new("WMVx",
            "Modern WoW Model Viewer (M2/WMO preview)",
            @"WMVx\WMVx",
            "VIEWERS", AllVersions,
            "https://github.com/Frostshake/WMVx", "Active"),

        new("Everlook",
            "Cross-platform WoW model viewer (libwarcraft)",
            @"Everlook\Everlook",
            "VIEWERS", AllMpq,
            "https://github.com/WowDevTools/Everlook", "Maintained"),

        new("wow.export",
            "Export models, textures, maps, sounds to OBJ/glTF",
            @"wowexport\wow.export",
            "VIEWERS", AllVersions,
            "https://github.com/Kruithne/wow.export", "Active"),

        new("wow.tools.local",
            "Local DBC/model browser (wow.tools offline)",
            @"wowtools\wow.tools.local",
            "VIEWERS", AllVersions,
            "https://github.com/Marlamin/wow.tools.local", "Active"),

        // ══════════════════════════════════════════════════
        //  GENERATORS
        // ══════════════════════════════════════════════════
        new("GObject Spawner",
            "Generate game object placement SQL",
            @"GobjectSpawner\GobjectSpawner",
            "GENERATORS", AllMpq),

        new("Minimap Gen",
            "Generate minimap tiles from ADT files",
            @"MinimapGenerator\MinimapGenerator",
            "GENERATORS", ClassicTrio),

        new("WoWHeightGen",
            "Generate heightmaps and minimaps for WoW maps",
            @"WoWHeightGen\WoWHeightGen",
            "GENERATORS", AllVersions,
            "https://github.com/CucFlavius/WoWHeightGen", "Active"),

        // ══════════════════════════════════════════════════
        //  PACKAGING
        // ══════════════════════════════════════════════════
        new("MPQ Editor",
            "Create and edit MPQ data archives",
            @"MPQEditor\MPQEditor",
            "PACKAGING", AllMpq,
            "http://www.zezula.net/en/mpq/download.html", "Active"),

        new("mpqcli",
            "CLI tool for MPQ create/extract/list/verify",
            @"mpqcli\mpqcli",
            "PACKAGING", AllMpq,
            "https://github.com/TheGrayDot/mpqcli", "Active"),

        new("CASCExplorer",
            "Browse and extract CASC archives",
            @"CASCExplorer\CASCExplorer",
            "PACKAGING", AllCasc,
            "https://github.com/WoW-Tools/CASCExplorer", "Active"),

        new("CASCHost",
            "Host modified CASC container for custom content",
            @"CASCHost\CASCHost",
            "PACKAGING", AllCasc,
            "https://github.com/WowDevTools/CASCHost", "Active"),

        // ══════════════════════════════════════════════════
        //  NETWORK
        // ══════════════════════════════════════════════════
        new("WowPacketParser",
            "Parse and analyze captured sniff files to SQL",
            @"WowPacketParser\WowPacketParser",
            "NETWORK", AllVersions,
            "https://github.com/TrinityCore/WowPacketParser", "Active"),

        new("ymir",
            "Modern TrinityCore packet sniffer",
            @"ymir\ymir",
            "NETWORK", AllVersions,
            "https://github.com/TrinityCore/ymir", "Active"),

        new("SzimatSzatyor",
            "Injection-based WoW packet capture",
            @"PacketSniffer\PacketSniffer",
            "NETWORK", AllMpq,
            "https://github.com/Anubisss/SzimatSzatyor", "Dormant"),

        // ══════════════════════════════════════════════════
        //  CLIENT PATCHING
        // ══════════════════════════════════════════════════
        new("RCE Patcher",
            "Patch known RCE exploit in 3.3.5a WoW.exe",
            @"RCEPatcher\RCEPatcher",
            "CLIENT PATCHING", WotlkOnly,
            "https://github.com/Gargash/WoW-RCE-Patcher", "Complete"),

        new("wow-patcher",
            "Rust-based client patcher for modern Classic",
            @"wowpatcher\wow-patcher",
            "CLIENT PATCHING", [WowVersion.Legion, WowVersion.BfA],
            "https://github.com/wowemulation-dev/wow-patcher", "Active"),

        new("Client Patcher 335",
            "Modular binary patcher for WoW.exe build 12340",
            @"ClientPatcher335\patcher",
            "CLIENT PATCHING", WotlkOnly,
            "https://github.com/Stormhand-dev/WoW-3.3.5-Patcher---Project-Reforged", "Active"),

        new("VanillaFixes",
            "Stutter fix and custom DLL loader for 1.12.1",
            @"VanillaFixes\VanillaFixes",
            "CLIENT PATCHING", VanillaOnly,
            "https://github.com/hannesmann/vanillafixes", "Active"),

        new("Arctium Launcher",
            "Client launcher for modern WoW private servers",
            @"ArctiumLauncher\Arctium",
            "CLIENT PATCHING", AllCasc,
            "https://github.com/Arctium/WoW-Launcher", "Active"),

        // ══════════════════════════════════════════════════
        //  RETROPORTING
        // ══════════════════════════════════════════════════
        new("Retroporting Scripts",
            "Python toolkit to retroport assets to 3.3.5a",
            @"retroporting\retroport",
            "RETROPORTING", AllVersions,
            "https://github.com/fischerlol/retroporting", "Maintained"),

        new("GOB Retroport",
            "Retroport game objects from newer expansions",
            @"gob_retroport\gob_retroport",
            "RETROPORTING", AllVersions,
            "https://github.com/GmFactoryWoW/gob_retroport", "Stale"),

        // ══════════════════════════════════════════════════
        //  SERVER ADMIN
        // ══════════════════════════════════════════════════
        new("AzerothAdmin",
            "In-game admin panel addon for AzerothCore",
            @"AzerothAdmin\AzerothAdmin",
            "SERVER ADMIN", WotlkOnly,
            "https://github.com/superstyro/AzerothAdmin", "Active"),

        new("TSWoW",
            "TypeScript modding framework with IDE and mod loader",
            @"tswow\tswow",
            "SERVER ADMIN", WotlkOnly,
            "https://github.com/tswow/tswow", "Active"),

        new("SPP Legion Admin",
            "In-game admin control panel addon for 7.3.5",
            @"SPPLegionAdmin\SPPLegionAdmin",
            "SERVER ADMIN", [WowVersion.Legion],
            "https://github.com/faustus1005/SPPLegionAdmin", "Active"),

        // ══════════════════════════════════════════════════
        //  LIBRARIES & CLI
        // ══════════════════════════════════════════════════
        new("warcraft-rs",
            "Rust CLI for all WoW file formats (MPQ/BLP/M2/WMO/ADT/DBC)",
            @"warcraft-rs\warcraft-rs",
            "LIBRARIES", AllMpq,
            "https://github.com/wowemulation-dev/warcraft-rs", "Active"),

        new("StormLib",
            "C/C++ library for reading/writing MPQ archives",
            @"StormLib\StormLib",
            "LIBRARIES", AllMpq,
            "https://github.com/ladislav-zezula/StormLib", "Active"),

        new("libwarcraft",
            ".NET library for all WoW data files",
            @"libwarcraft\libwarcraft",
            "LIBRARIES", AllVersions,
            "https://github.com/WowDevTools/libwarcraft", "Maintained"),

        new("pywowlib",
            "Python library for WoW file formats",
            @"pywowlib\launch.cmd",
            "LIBRARIES", AllVersions,
            "https://github.com/wowdev/pywowlib", "Active"),

        new("Warcraft.NET",
            "C# library for WoW binary file formats",
            @"WarCraftNET\Warcraft.NET",
            "LIBRARIES", AllVersions,
            "https://github.com/ModernWoWTools/Warcraft.NET", "Active"),

        new("namigator",
            "High-performance pathfinding API for WoW maps",
            @"namigator\namigator",
            "LIBRARIES", ClassicTrio,
            "https://github.com/namreeb/namigator", "Active"),

        new("CASCLib",
            "C/C++ library for reading CASC game archives",
            @"CASCLib\CASCLib",
            "LIBRARIES", AllCasc,
            "https://github.com/ladislav-zezula/CascLib", "Active"),

        new("DBCD",
            ".NET library for reading/writing all DB2 variants",
            @"DBCD\DBCD",
            "LIBRARIES", CataPlus,
            "https://github.com/wowdev/DBCD", "Active"),

        new("WoWDBDefs",
            "Community DB2 structure definitions for all versions",
            @"WoWDBDefs\WoWDBDefs",
            "LIBRARIES", AllVersions,
            "https://github.com/wowdev/WoWDBDefs", "Active"),

        // ══════════════════════════════════════════════════
        //  ADT CLI TOOLS
        // ══════════════════════════════════════════════════
        new("Detail Doodads",  "Add ground clutter doodads per texture",    @"CLI\AddDetailDoodads",
            "ADT CLI TOOLS", ClassicTrio),
        new("All Ocean",       "Fill ADT with ocean-style water",           @"CLI\AllOcean",
            "ADT CLI TOOLS", ClassicTrio),
        new("All Water",       "Add flat water layer at specified height",  @"CLI\AllWater",
            "ADT CLI TOOLS", ClassicTrio),
        new("Cover Water",     "Cover terrain with depth-matched water",    @"CLI\CoverWithWater",
            "ADT CLI TOOLS", ClassicTrio),
        new("Create WDT",      "Generate WDT file for ADT tile range",     @"CLI\CreateWDT",
            "ADT CLI TOOLS", ClassicTrio),
        new("ADT Info",        "Export ADT metadata to text file",          @"CLI\FileInfo",
            "ADT CLI TOOLS", ClassicTrio),
        new("Fix Ocean",       "Remove all MCLQ water chunks",             @"CLI\FixAllOcean",
            "ADT CLI TOOLS", ClassicTrio),
        new("Load Info",       "Import text file data back into ADT",      @"CLI\LoadInfo",
            "ADT CLI TOOLS", ClassicTrio),
        new("Slope Picture",   "Generate terrain slope visualization",     @"CLI\GetSlopePicture",
            "ADT CLI TOOLS", ClassicTrio),
        new("Make Map",        "Combine multiple RAW maps into one",       @"CLI\MakeMap",
            "ADT CLI TOOLS", ClassicTrio),
        new("Map Mover",       "Relocate ADT tiles to new coordinates",    @"CLI\MapMover",
            "ADT CLI TOOLS", ClassicTrio),
        new("Model Swap",      "Replace model filename references in ADT", @"CLI\ModelSwap",
            "ADT CLI TOOLS", ClassicTrio),
        new("Chunk Flags",     "Set MCNK water/magma/ocean flags",         @"CLI\SetChunkFlags",
            "ADT CLI TOOLS", ClassicTrio),
        new("Raise Terrain",   "Shift all terrain vertices by height",     @"CLI\RaiseTerrain",
            "ADT CLI TOOLS", ClassicTrio),
        new("Remove Sound",    "Delete all sound emitters from ADT",       @"CLI\RemoveSound",
            "ADT CLI TOOLS", ClassicTrio),
        new("Patch Holes",     "Remove all terrain holes in ADT",          @"CLI\PatchHoles",
            "ADT CLI TOOLS", ClassicTrio),
        new("Offset Fix",      "Fix ADT offsets after tile relocation",    @"CLI\OffsetFix",
            "ADT CLI TOOLS", ClassicTrio),
        new("Remove Walls",    "Clear impassable terrain chunks",          @"CLI\RemoveTheWalls",
            "ADT CLI TOOLS", ClassicTrio),
    ];

    public static ToolEntry[] GetToolsForVersion(WowVersion version) =>
        version == WowVersion.All
            ? GetAllTools()
            : GetAllTools().Where(t => t.IsCompatible(version)).ToArray();

    public static (string category, ToolEntry[] tools)[] GetGroupedTools(WowVersion version)
    {
        var tools = GetToolsForVersion(version);
        return tools
            .GroupBy(t => t.Category)
            .Select(g => (g.Key, g.ToArray()))
            .ToArray();
    }

    public static readonly string[] CategoryOrder =
    [
        "WORLD EDITING",
        "DATA EDITING",
        "MODEL TOOLS",
        "VIEWERS",
        "GENERATORS",
        "PACKAGING",
        "NETWORK",
        "CLIENT PATCHING",
        "RETROPORTING",
        "SERVER ADMIN",
        "LIBRARIES",
        "ADT CLI TOOLS"
    ];
}
