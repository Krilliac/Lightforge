namespace Lightforge;

public class ProjectTemplate
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Expansion { get; init; } = "WotLK 3.3.5a";
    public Dictionary<string, string> StarterFiles { get; init; } = new();

    public void Apply(string projectDir)
    {
        foreach (var (relPath, content) in StarterFiles)
        {
            var fullPath = Path.Combine(projectDir, relPath);
            var dir = Path.GetDirectoryName(fullPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(fullPath, content);
        }
    }
}

public static class ProjectTemplates
{
    public static readonly ProjectTemplate[] All =
    [
        new ProjectTemplate
        {
            Id = "custom-map",
            Name = "Custom Map",
            Description = "Starter project for custom terrain/map editing with ADT placeholders",
            Expansion = "WotLK 3.3.5a",
            StarterFiles = new()
            {
                ["Config/map-notes.txt"] =
                    "# Custom Map Notes\n\nMap ID: (assign in worlddb `map` table)\nContinent: (e.g. Azeroth=0, Kalimdor=1, custom=N)\n\n## Coordinates\nCenter: X, Y\nBounds: ...\n",
                ["SQL/01_map_entry.sql"] =
                    "-- Create map entry in the `map` table\n-- Adjust map ID to avoid conflicts with existing maps\n\n-- INSERT INTO `map` (`id`, `directory`, `instanceType`, `name`, `minLevel`, `maxLevel`)\n-- VALUES (999, 'CustomMap', 0, 'My Custom Map', 1, 80);\n",
                ["SQL/02_teleport.sql"] =
                    "-- Teleport command for testing\n-- .tele name CustomMap\n\n-- INSERT INTO `game_tele` (`id`, `position_x`, `position_y`, `position_z`, `orientation`, `map`, `name`)\n-- VALUES (9999, 0, 0, 100, 0, 999, 'CustomMap');\n",
                ["Patches/readme.txt"] =
                    "Place your custom ADT files here.\nThey will be deployed to WoW Data/ on build.\n\nTypical structure:\n  World/Maps/CustomMap/CustomMap_XX_YY.adt\n",
            }
        },
        new ProjectTemplate
        {
            Id = "item-pack",
            Name = "Item Pack",
            Description = "Custom items with DBC patches, models, and SQL for TrinityCore/AzerothCore",
            Expansion = "WotLK 3.3.5a",
            StarterFiles = new()
            {
                ["SQL/01_items.sql"] =
                    "-- Custom item definitions\n-- Use entry IDs above 100000 to avoid conflicts\n\n/*\nINSERT INTO `item_template`\n  (`entry`, `class`, `subclass`, `name`, `displayid`,\n   `Quality`, `Flags`, `BuyPrice`, `SellPrice`,\n   `InventoryType`, `AllowableClass`, `AllowableRace`,\n   `ItemLevel`, `RequiredLevel`,\n   `stat_type1`, `stat_value1`,\n   `dmg_min1`, `dmg_max1`, `dmg_type1`,\n   `delay`, `description`)\nVALUES\n  (100001, 2, 7, 'Custom Sword', 0,\n   3, 0, 50000, 25000,\n   17, -1, -1,\n   80, 60,\n   7, 20,\n   80, 150, 0,\n   2600, 'A custom crafted blade');\n*/\n",
                ["SQL/02_vendor.sql"] =
                    "-- Add items to a vendor\n-- Use creature entry for your custom NPC\n\n-- INSERT INTO `npc_vendor` (`entry`, `item`, `maxcount`, `incrtime`, `ExtendedCost`)\n-- VALUES (100001, 100001, 0, 0, 0);\n",
                ["Config/items.csv"] =
                    "entry,name,quality,ilvl,required_level,notes\n100001,Custom Sword,Rare,80,60,Two-hand sword\n100002,Custom Shield,Uncommon,75,55,Off-hand shield\n",
                ["Textures/readme.txt"] =
                    "Place custom item icon BLP files here.\nIcons go in: Interface/Icons/\n",
                ["Models/readme.txt"] =
                    "Place custom item M2 model files here.\nWeapon models go in: Item/ObjectComponents/Weapon/\n",
            }
        },
        new ProjectTemplate
        {
            Id = "server-repack",
            Name = "Server Repack",
            Description = "Server-side content: NPCs, quests, spawns, and SmartAI scripts",
            Expansion = "WotLK 3.3.5a",
            StarterFiles = new()
            {
                ["SQL/01_creatures.sql"] =
                    "-- Custom creature definitions\n-- Use entry IDs above 100000\n\n/*\nINSERT INTO `creature_template`\n  (`entry`, `name`, `subname`, `minlevel`, `maxlevel`,\n   `faction`, `npcflag`, `modelid1`, `AIName`, `ScriptName`)\nVALUES\n  (100001, 'Custom NPC', 'Quest Giver', 80, 80,\n   35, 3, 15784, 'SmartAI', '');\n*/\n",
                ["SQL/02_quests.sql"] =
                    "-- Custom quest definitions\n\n/*\nINSERT INTO `quest_template`\n  (`ID`, `QuestType`, `QuestLevel`, `MinLevel`, `Title`,\n   `LogDescription`, `QuestDescription`,\n   `RewardChoiceItemID1`, `RewardChoiceItemQuantity1`)\nVALUES\n  (100001, 2, 80, 75, 'A Custom Quest',\n   'Speak with the NPC in town.',\n   'The NPC needs your help with something important.',\n   100001, 1);\n*/\n",
                ["SQL/03_spawns.sql"] =
                    "-- Creature spawns\n-- Position: use .gps in-game to get coordinates\n\n-- INSERT INTO `creature` (`guid`, `id1`, `map`, `position_x`, `position_y`, `position_z`, `orientation`, `spawntimesecs`)\n-- VALUES (900001, 100001, 0, -8949.95, -132.493, 83.5312, 0, 300);\n",
                ["SQL/04_smartai.sql"] =
                    "-- SmartAI scripts for custom NPCs\n-- See: https://trinitycore.info/en/database/335/world/smart_scripts\n\n-- DELETE FROM `smart_scripts` WHERE `entryorguid` = 100001;\n-- INSERT INTO `smart_scripts` (`entryorguid`, `source_type`, `id`, `link`,\n--   `event_type`, `event_phase_mask`, `event_chance`,\n--   `event_flags`, `event_param1`, `event_param2`,\n--   `action_type`, `action_param1`, `target_type`,\n--   `comment`)\n-- VALUES\n-- (100001, 0, 0, 0, 1, 0, 100, 0, 1000, 5000, 11, 12345, 2, 'Custom NPC - Cast spell');\n",
                ["Config/worlddb-notes.txt"] =
                    "# Server Content Notes\n\nTarget core: TrinityCore / AzerothCore (WotLK 3.3.5a)\n\n## ID Ranges (avoid conflicts)\n- Creatures: 100001-109999\n- Quests: 100001-109999  \n- Items: 100001-109999\n- GUIDs: 900001-909999\n\n## Apply order\n1. creatures\n2. quests\n3. spawns\n4. smartai\n",
            }
        },
        new ProjectTemplate
        {
            Id = "addon",
            Name = "Addon Project",
            Description = "Client-side Lua addon with TOC, slash commands, and locale support",
            Expansion = "WotLK 3.3.5a",
            StarterFiles = new()
            {
                ["Lua/MyAddon/MyAddon.toc"] =
                    "## Interface: 30300\n## Title: MyAddon\n## Notes: A custom addon\n## Author: Unknown\n## Version: 1.0.0\n## DefaultState: enabled\n\nLocale\\enUS.lua\nMyAddon.lua\n",
                ["Lua/MyAddon/MyAddon.lua"] =
                    "local addonName, ns = ...\n\nlocal frame = CreateFrame(\"Frame\")\nframe:RegisterEvent(\"PLAYER_LOGIN\")\nframe:SetScript(\"OnEvent\", function(self, event)\n    print(\"|cff00ff00\" .. addonName .. \"|r loaded.\")\nend)\n\nSLASH_MYADDON1 = \"/myaddon\"\nSlashCmdList[\"MYADDON\"] = function(msg)\n    print(\"|cff00ff00MyAddon|r - type /myaddon help\")\nend\n",
                ["Lua/MyAddon/Locale/enUS.lua"] =
                    "-- MyAddon Locale: enUS\nlocal L = {}\nL[\"LOADED\"] = \"|cff00ff00MyAddon|r loaded.\"\n",
                ["Config/addon-notes.txt"] =
                    "# Addon Development Notes\n\nCopy the Lua/MyAddon/ folder to:\n  <WoW>/Interface/AddOns/MyAddon/\n\nReload in-game: /reload\nTest: /myaddon\n",
            }
        },
        new ProjectTemplate
        {
            Id = "texture-pack",
            Name = "Texture Pack",
            Description = "Custom textures and BLP files for retexturing terrain, items, or UI",
            Expansion = "WotLK 3.3.5a",
            StarterFiles = new()
            {
                ["Textures/readme.txt"] =
                    "# Texture Pack\n\nOrganize BLP files by type:\n  terrain/   - Ground textures (Tileset/)\n  items/     - Item icons (Interface/Icons/)\n  ui/        - UI elements (Interface/)\n  models/    - Model textures (alongside M2/WMO)\n\nUse BLPConverter or BLP Lab to convert PNG/TGA → BLP.\n",
                ["Patches/readme.txt"] =
                    "Place converted BLP files in the correct path structure here.\nThey will be deployed to WoW Data/ on build.\n\nExample paths:\n  Tileset/Generic/Black_256.blp\n  Interface/Icons/INV_Custom_01.blp\n",
                ["Config/texture-list.csv"] =
                    "original_path,replacement,notes\n,Tileset/Generic/Custom_Ground.blp,New ground texture\n,Interface/Icons/INV_Custom_Sword.blp,Custom sword icon\n",
            }
        },
    ];
}
