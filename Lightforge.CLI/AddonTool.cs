namespace Lightforge;

static class AddonTool
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 1;
        }

        var name = args[0];
        var parentDir = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
        var author = "Unknown";
        var version = "1.0.0";
        bool withSavedVars = false;
        bool withSlash = true;
        bool withXml = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--author" or "-a" when i + 1 < args.Length:
                    author = args[++i];
                    break;
                case "--version" when i + 1 < args.Length:
                    version = args[++i];
                    break;
                case "--saved-vars":
                    withSavedVars = true;
                    break;
                case "--no-slash":
                    withSlash = false;
                    break;
                case "--xml":
                    withXml = true;
                    break;
                case "--help" or "-h":
                    PrintHelp();
                    return 0;
            }
        }

        return Generate(name, parentDir, author, version, withSavedVars, withSlash, withXml);
    }

    static void PrintHelp()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Addon Scaffolding\n");
        Console.ResetColor();
        Console.WriteLine("Usage: lightforge new-addon <name> [path] [options]\n");
        Console.WriteLine("Generates a WoW addon skeleton with TOC, Lua, and optional XML.\n");
        Console.WriteLine("Options:");
        Console.WriteLine("  --author, -a <name>  Addon author (default: Unknown)");
        Console.WriteLine("  --version <ver>      Addon version (default: 1.0.0)");
        Console.WriteLine("  --saved-vars         Add SavedVariables support");
        Console.WriteLine("  --no-slash            Omit slash command handler");
        Console.WriteLine("  --xml                Include XML frame template");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  lightforge new-addon MyAddon");
        Console.WriteLine("  lightforge new-addon CoolUI --author Krilliac --saved-vars --xml");
    }

    static int Generate(string name, string parentDir, string author,
        string version, bool savedVars, bool slash, bool xml)
    {
        var addonDir = Path.Combine(parentDir, name);
        if (Directory.Exists(addonDir))
        {
            Console.Error.WriteLine($"Directory already exists: {addonDir}");
            return 1;
        }

        Directory.CreateDirectory(addonDir);

        var tocContent = GenerateToc(name, author, version, savedVars, xml);
        File.WriteAllText(Path.Combine(addonDir, $"{name}.toc"), tocContent);

        var coreContent = GenerateCoreLua(name, slash, savedVars);
        File.WriteAllText(Path.Combine(addonDir, $"{name}.lua"), coreContent);

        if (xml)
        {
            var xmlContent = GenerateXml(name);
            File.WriteAllText(Path.Combine(addonDir, $"{name}.xml"), xmlContent);
        }

        var localeDir = Path.Combine(addonDir, "Locale");
        Directory.CreateDirectory(localeDir);
        File.WriteAllText(Path.Combine(localeDir, "enUS.lua"),
            $"-- {name} Locale: enUS\nlocal L = {{}}\nL[\"LOADED\"] = \"|cff00ff00{name}|r loaded.\"\n");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("Created");
        Console.ResetColor();
        Console.WriteLine($" addon \"{name}\" at {addonDir}\n");

        Console.WriteLine("Files:");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"  {name}.toc");
        Console.ResetColor();
        Console.WriteLine("          Table of contents");

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"  {name}.lua");
        Console.ResetColor();
        Console.WriteLine("          Core addon logic");

        if (xml)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"  {name}.xml");
            Console.ResetColor();
            Console.WriteLine("          UI frame template");
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write("  Locale/enUS.lua");
        Console.ResetColor();
        Console.WriteLine("    Localization strings");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\nCopy {name}/ to your WoW Interface/AddOns/ directory to install.");
        Console.ResetColor();

        return 0;
    }

    static string GenerateToc(string name, string author, string version,
        bool savedVars, bool xml)
    {
        var lines = new List<string>
        {
            $"## Interface: 30300",
            $"## Title: {name}",
            $"## Notes: A custom WoW addon",
            $"## Author: {author}",
            $"## Version: {version}",
        };

        if (savedVars)
            lines.Add($"## SavedVariables: {name}DB");

        lines.Add($"## DefaultState: enabled");
        lines.Add("");
        lines.Add($"Locale\\enUS.lua");

        if (xml)
            lines.Add($"{name}.xml");

        lines.Add($"{name}.lua");
        lines.Add("");

        return string.Join("\n", lines);
    }

    static string GenerateCoreLua(string name, bool slash, bool savedVars)
    {
        var lines = new List<string>
        {
            $"local addonName, ns = ...",
            $"",
            $"local frame = CreateFrame(\"Frame\")",
            $"frame:RegisterEvent(\"ADDON_LOADED\")",
            $"frame:RegisterEvent(\"PLAYER_LOGIN\")",
            $"",
            $"frame:SetScript(\"OnEvent\", function(self, event, ...)",
            $"    if event == \"ADDON_LOADED\" then",
            $"        local loadedAddon = ...",
            $"        if loadedAddon == addonName then",
        };

        if (savedVars)
        {
            lines.Add($"            {name}DB = {name}DB or {{}}");
            lines.Add($"            ns.db = {name}DB");
        }

        lines.AddRange([
            $"            self:UnregisterEvent(\"ADDON_LOADED\")",
            $"        end",
            $"    elseif event == \"PLAYER_LOGIN\" then",
            $"        print(\"|cff00ff00{name}|r loaded.\")",
            $"    end",
            $"end)",
        ]);

        if (slash)
        {
            lines.AddRange([
                $"",
                $"SLASH_{name.ToUpperInvariant()}1 = \"/{name.ToLowerInvariant()}\"",
                $"SlashCmdList[\"{name.ToUpperInvariant()}\"] = function(msg)",
                $"    local cmd, rest = msg:match(\"^(%S+)%s*(.-)$\")",
                $"    if not cmd then",
                $"        print(\"|cff00ff00{name}|r - Available commands:\")",
                $"        print(\"  /{name.ToLowerInvariant()} help\")",
                $"        return",
                $"    end",
                $"    cmd = cmd:lower()",
                $"    if cmd == \"help\" then",
                $"        print(\"|cff00ff00{name}|r Commands:\")",
                $"        print(\"  /{name.ToLowerInvariant()} help - Show this help\")",
                $"    end",
                $"end",
            ]);
        }

        lines.Add("");
        return string.Join("\n", lines);
    }

    static string GenerateXml(string name)
    {
        return $"""
            <Ui xmlns="http://www.blizzard.com/wow/ui/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                xsi:schemaLocation="http://www.blizzard.com/wow/ui/ ..\FrameXML\UI.xsd">

                <Frame name="{name}MainFrame" parent="UIParent" hidden="true">
                    <Size x="300" y="200"/>
                    <Anchors>
                        <Anchor point="CENTER"/>
                    </Anchors>
                    <Backdrop bgFile="Interface\DialogFrame\UI-DialogBox-Background"
                              edgeFile="Interface\DialogFrame\UI-DialogBox-Border" tile="true">
                        <BackgroundInsets>
                            <AbsInset left="11" right="12" top="12" bottom="11"/>
                        </BackgroundInsets>
                        <TileSize>
                            <AbsValue val="32"/>
                        </TileSize>
                        <EdgeSize>
                            <AbsValue val="32"/>
                        </EdgeSize>
                    </Backdrop>
                    <Layers>
                        <Layer level="OVERLAY">
                            <FontString name="$parentTitle" inherits="GameFontNormalLarge"
                                        text="{name}">
                                <Anchors>
                                    <Anchor point="TOP" relativePoint="TOP">
                                        <Offset><AbsDimension x="0" y="-16"/></Offset>
                                    </Anchor>
                                </Anchors>
                            </FontString>
                        </Layer>
                    </Layers>
                    <Frames>
                        <Button name="$parentCloseButton" inherits="UIPanelCloseButton">
                            <Anchors>
                                <Anchor point="TOPRIGHT" relativePoint="TOPRIGHT">
                                    <Offset><AbsDimension x="-4" y="-4"/></Offset>
                                </Anchor>
                            </Anchors>
                        </Button>
                    </Frames>
                </Frame>
            </Ui>
            """;
    }
}
