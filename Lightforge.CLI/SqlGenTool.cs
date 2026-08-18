namespace Lightforge;

static class SqlGenTool
{
    public static int Generate(string[] args)
    {
        if (args.Length == 0)
        {
            PrintTemplateList();
            return 0;
        }

        var template = args[0].ToLowerInvariant();
        string? outputPath = null;
        uint entryId = 90000;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--id" when i + 1 < args.Length:
                    if (uint.TryParse(args[++i], out uint id)) entryId = id;
                    break;
                case "-o" or "--output" when i + 1 < args.Length:
                    outputPath = args[++i];
                    break;
            }
        }

        string? sql = template switch
        {
            "item" => ItemTemplate(entryId),
            "creature" or "npc" => CreatureTemplate(entryId),
            "quest" => QuestTemplate(entryId),
            "spawn" or "creature-spawn" => CreatureSpawn(entryId),
            "gobj" or "gameobject" or "gob-spawn" => GameobjectSpawn(entryId),
            "vendor" or "npc-vendor" => NpcVendor(entryId),
            "loot" => LootTemplate(entryId),
            "gossip" => GossipMenu(entryId),
            "trainer" => NpcTrainer(entryId),
            "waypoint" => Waypoints(entryId),
            "smartai" or "smart" => SmartAI(entryId),
            _ => null
        };

        if (sql == null)
        {
            Console.Error.WriteLine($"Unknown template: {template}");
            Console.Error.WriteLine("Run 'lightforge sql-gen' to see available templates.");
            return 1;
        }

        if (outputPath != null)
        {
            File.WriteAllText(outputPath, sql);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Saved");
            Console.ResetColor();
            Console.WriteLine($" {template} template to {outputPath}");
        }
        else
        {
            Console.WriteLine(sql);
        }

        return 0;
    }

    static void PrintTemplateList()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  SQL Template Generator\n");
        Console.ResetColor();

        Console.WriteLine("Usage: lightforge sql-gen <template> [--id N] [-o file.sql]\n");
        Console.WriteLine("Templates:");
        PrintTemplate("item", "Item definition (item_template)");
        PrintTemplate("creature", "NPC definition (creature_template)");
        PrintTemplate("quest", "Quest definition (quest_template)");
        PrintTemplate("spawn", "Creature spawn point (creature)");
        PrintTemplate("gobj", "Gameobject spawn (gameobject)");
        PrintTemplate("vendor", "NPC vendor items (npc_vendor)");
        PrintTemplate("loot", "Loot table entries (creature_loot_template)");
        PrintTemplate("gossip", "Gossip menu and options");
        PrintTemplate("trainer", "NPC trainer spells (npc_trainer)");
        PrintTemplate("waypoint", "Patrol waypoints (waypoint_data)");
        PrintTemplate("smartai", "SmartAI scripted event");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\nOptions:");
        Console.ResetColor();
        Console.WriteLine("  --id N      Starting entry ID (default: 90000)");
        Console.WriteLine("  -o file     Write to file instead of stdout");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\nAll templates target TrinityCore / AzerothCore schema.");
        Console.ResetColor();
    }

    static void PrintTemplate(string name, string desc)
    {
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(name.PadRight(14));
        Console.ResetColor();
        Console.WriteLine(desc);
    }

    static string ItemTemplate(uint id) => $@"-- Item Template (item_template)
-- Entry: {id}
-- Docs: https://trinitycore.info/en/database/335/world/item_template

DELETE FROM `item_template` WHERE `entry` = {id};
INSERT INTO `item_template` (
    `entry`, `class`, `subclass`, `SoundOverrideSubclass`,
    `name`, `displayid`, `Quality`, `Flags`,
    `BuyCount`, `BuyPrice`, `SellPrice`,
    `InventoryType`, `AllowableClass`, `AllowableRace`,
    `ItemLevel`, `RequiredLevel`,
    `stat_type1`, `stat_value1`,
    `stat_type2`, `stat_value2`,
    `stat_type3`, `stat_value3`,
    `dmg_min1`, `dmg_max1`, `dmg_type1`,
    `armor`, `delay`,
    `bonding`, `description`,
    `stackable`, `maxcount`,
    `Material`, `sheath`
) VALUES (
    {id},
    2,          -- class: 2=Weapon, 4=Armor
    7,          -- subclass (weapon): 0=Axe1H, 1=Axe2H, 4=Mace1H, 7=Sword1H, 8=Sword2H
    -1,         -- SoundOverrideSubclass
    'Custom Item Name',
    0,          -- displayid (model)
    3,          -- Quality: 0=Gray, 1=White, 2=Green, 3=Blue, 4=Purple, 5=Orange
    0,          -- Flags
    1,          -- BuyCount
    100000,     -- BuyPrice (in copper, 10g)
    25000,      -- SellPrice (in copper, 2g50s)
    13,         -- InventoryType: 1=Head, 3=Shoulder, 5=Chest, 13=OneHand, 17=TwoHand
    -1,         -- AllowableClass (-1 = all)
    -1,         -- AllowableRace (-1 = all)
    80,         -- ItemLevel
    70,         -- RequiredLevel
    3,  10,     -- stat_type1: 3=Agility, stat_value1
    7,  15,     -- stat_type2: 7=Stamina, stat_value2
    32, 8,      -- stat_type3: 32=CritRating, stat_value3
    50.0, 100.0, 0,  -- dmg_min1, dmg_max1, dmg_type1 (0=Physical)
    0,          -- armor
    2600,       -- delay (attack speed in ms)
    1,          -- bonding: 0=None, 1=BoP, 2=BoE, 3=BoU
    'A custom item created with Lightforge',
    1,          -- stackable
    1,          -- maxcount
    1,          -- Material: 0=Undefined, 1=Metal, 2=Wood, 3=Liquid, 4=Jewelry
    3           -- sheath: 0=None, 1=TwoHandWeapon, 2=Staff, 3=OneHand
);
";

    static string CreatureTemplate(uint id) => $@"-- Creature Template (creature_template)
-- Entry: {id}
-- Docs: https://trinitycore.info/en/database/335/world/creature_template

DELETE FROM `creature_template` WHERE `entry` = {id};
INSERT INTO `creature_template` (
    `entry`, `name`, `subname`, `modelid1`,
    `minlevel`, `maxlevel`, `faction`, `npcflag`,
    `speed_walk`, `speed_run`,
    `mingold`, `maxgold`,
    `AIName`, `MovementType`,
    `HealthModifier`, `ManaModifier`,
    `DamageModifier`, `ArmorModifier`,
    `type`, `type_flags`,
    `lootid`, `skinloot`, `pickpocketloot`,
    `mechanic_immune_mask`,
    `unit_class`, `unit_flags`
) VALUES (
    {id},
    'Custom NPC Name',
    'Title / Subname',
    0,          -- modelid1 (display ID)
    70, 70,     -- minlevel, maxlevel
    35,         -- faction (35 = Friendly to all)
    0,          -- npcflag: 1=Gossip, 2=QuestGiver, 128=Vendor, 16=Trainer
    1.0, 1.14,  -- speed_walk, speed_run
    0, 0,       -- mingold, maxgold (loot copper)
    '',         -- AIName: '' or 'SmartAI'
    0,          -- MovementType: 0=Idle, 1=Random, 2=Waypoint
    1.0, 1.0,   -- HealthModifier, ManaModifier
    1.0, 1.0,   -- DamageModifier, ArmorModifier
    7,          -- type: 1=Beast, 7=Humanoid, 10=Undead
    0,          -- type_flags
    0, 0, 0,    -- lootid, skinloot, pickpocketloot
    0,          -- mechanic_immune_mask
    1,          -- unit_class: 1=Warrior, 2=Paladin, 8=Mage
    0           -- unit_flags
);
";

    static string QuestTemplate(uint id) => $@"-- Quest Template (quest_template)
-- Entry: {id}
-- Docs: https://trinitycore.info/en/database/335/world/quest_template

DELETE FROM `quest_template` WHERE `ID` = {id};
INSERT INTO `quest_template` (
    `ID`, `QuestType`, `QuestLevel`, `MinLevel`, `QuestSortID`,
    `QuestInfoID`,
    `SuggestedGroupNum`,
    `RequiredFactionId1`, `RequiredFactionValue1`,
    `RewardNextQuest`,
    `RewardXPDifficulty`,
    `RewardMoney`,
    `RewardItem1`, `RewardAmount1`,
    `RewardChoiceItemID1`, `RewardChoiceItemQuantity1`,
    `StartItem`,
    `Flags`,
    `LogTitle`, `LogDescription`, `QuestDescription`,
    `AreaDescription`, `QuestCompletionLog`
) VALUES (
    {id},
    2,          -- QuestType: 1=Group, 2=Normal, 81=Daily, 82=Weekly
    70,         -- QuestLevel (-1 = scales)
    68,         -- MinLevel
    0,          -- QuestSortID (zone ID, negative = category)
    0,          -- QuestInfoID: 1=Group, 21=Life, 41=PvP, 62=Raid, 81=Dungeon, 82=WorldEvent
    0,          -- SuggestedGroupNum
    0, 0,       -- RequiredFactionId1, RequiredFactionValue1
    0,          -- RewardNextQuest (chain to next quest ID)
    5,          -- RewardXPDifficulty (0-9, lookup in quest_xp table)
    50000,      -- RewardMoney (copper, 5g. Negative = required)
    0, 0,       -- RewardItem1, RewardAmount1
    0, 0,       -- RewardChoiceItemID1, RewardChoiceItemQuantity1
    0,          -- StartItem (provided item)
    0,          -- Flags: 1=Stay, 2=Escort, 4=Exploration, 8=Sharable
    'Custom Quest Title',
    'Description shown in quest log.',
    'Detailed objectives text shown when talking to NPC.',
    '',
    'Return to the quest giver.'
);

-- Quest objectives: kill creatures or collect items
DELETE FROM `quest_template_addon` WHERE `ID` = {id};
INSERT INTO `quest_template_addon` (`ID`, `MaxLevel`, `AllowableClasses`)
VALUES ({id}, 0, 0);  -- 0 = no restriction

-- Required kills/items (quest_objectives for TC, creature_queststarter for AC)
-- Add quest giver: INSERT INTO creature_queststarter (id, quest) VALUES (<npc_entry>, {id});
-- Add quest ender: INSERT INTO creature_questender (id, quest) VALUES (<npc_entry>, {id});
";

    static string CreatureSpawn(uint id) => $@"-- Creature Spawn (creature table)
-- Spawns creature entry {id} in the world
-- Use .gps in-game to get coordinates

DELETE FROM `creature` WHERE `guid` = @GUID;
SET @GUID := (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `creature`);

INSERT INTO `creature` (
    `guid`, `id1`, `map`, `zoneId`, `areaId`,
    `position_x`, `position_y`, `position_z`, `orientation`,
    `spawntimesecs`, `wander_distance`,
    `MovementType`, `equipment_id`
) VALUES (
    @GUID,
    {id},           -- creature_template entry
    0,              -- map: 0=Eastern Kingdoms, 1=Kalimdor, 530=Outland, 571=Northrend
    0,              -- zoneId (auto-set, can be 0)
    0,              -- areaId (auto-set, can be 0)
    -8949.95,       -- position_x (Stormwind example)
    -132.493,       -- position_y
    83.5312,        -- position_z
    0.0,            -- orientation (0-2pi radians)
    300,            -- spawntimesecs (5 min respawn)
    0,              -- wander_distance (0 = stationary)
    0,              -- MovementType: 0=Idle, 1=Random, 2=Waypoint
    0               -- equipment_id
);
";

    static string GameobjectSpawn(uint id) => $@"-- Gameobject Spawn (gameobject table)
-- Spawns gameobject entry {id} in the world

SET @GUID := (SELECT IFNULL(MAX(`guid`), 0) + 1 FROM `gameobject`);

INSERT INTO `gameobject` (
    `guid`, `id`, `map`,
    `position_x`, `position_y`, `position_z`,
    `orientation`, `rotation2`, `rotation3`,
    `spawntimesecs`, `state`
) VALUES (
    @GUID,
    {id},           -- gameobject_template entry
    0,              -- map: 0=EK, 1=Kalimdor
    -8949.95,       -- position_x
    -132.493,       -- position_y
    83.5312,        -- position_z
    0.0,            -- orientation
    0.0, 0.0,       -- rotation2, rotation3 (quaternion)
    120,            -- spawntimesecs
    1               -- state: 0=active, 1=ready
);
";

    static string NpcVendor(uint id) => $@"-- NPC Vendor Items (npc_vendor)
-- Add items to creature entry {id}'s vendor list
-- Creature must have npcflag 128 (Vendor) set

-- Set vendor flag on creature
UPDATE `creature_template` SET `npcflag` = `npcflag` | 128 WHERE `entry` = {id};

DELETE FROM `npc_vendor` WHERE `entry` = {id};
INSERT INTO `npc_vendor` (`entry`, `item`, `maxcount`, `incrtime`, `ExtendedCost`) VALUES
({id}, 2589,  0,    0, 0),  -- Linen Cloth (unlimited stock)
({id}, 4306,  0,    0, 0),  -- Silk Cloth
({id}, 14047, 5, 3600, 0),  -- Runecloth Bag (5 stock, 1hr restock)
({id}, 38082, 1, 7200, 0);  -- Enchanting Vellum (1 stock, 2hr restock)
-- maxcount 0 = unlimited, incrtime = restock interval in seconds
-- ExtendedCost = reference to item_extended_cost for token purchases
";

    static string LootTemplate(uint id) => $@"-- Creature Loot Table (creature_loot_template)
-- Drops for creature entry {id}
-- Set creature_template.lootid = {id}

UPDATE `creature_template` SET `lootid` = {id} WHERE `entry` = {id};

DELETE FROM `creature_loot_template` WHERE `Entry` = {id};
INSERT INTO `creature_loot_template` (`Entry`, `Item`, `Reference`, `Chance`, `QuestRequired`, `LootMode`, `GroupId`, `MinCount`, `MaxCount`) VALUES
({id}, 29434, 0, 80.0, 0, 1, 0, 1, 3),  -- Badge of Justice (80% drop, 1-3)
({id}, 32228, 0, 5.0,  0, 1, 1, 1, 1),  -- Rare drop group 1 (5%)
({id}, 32230, 0, 5.0,  0, 1, 1, 1, 1),  -- Rare drop group 1 (5%, exclusive with above)
({id}, 24401, 0, 100,  1, 1, 0, 1, 1);  -- Quest item (100% if quest active)
-- GroupId 0 = independent roll per item
-- GroupId 1+ = exclusive group (only one drops from group)
-- QuestRequired 1 = only drops if player has an active quest needing it
";

    static string GossipMenu(uint id) => $@"-- Gossip Menu and Options
-- Creature entry {id} gossip setup
-- Creature must have npcflag 1 (Gossip) set

UPDATE `creature_template` SET `npcflag` = `npcflag` | 1 WHERE `entry` = {id};

-- Main gossip text
DELETE FROM `npc_text` WHERE `ID` = {id};
INSERT INTO `npc_text` (`ID`, `text0_0`) VALUES
({id}, 'Greetings, traveler. How may I assist you?');

-- Gossip menu
DELETE FROM `gossip_menu` WHERE `MenuID` = {id};
INSERT INTO `gossip_menu` (`MenuID`, `TextID`) VALUES ({id}, {id});

-- Menu options
DELETE FROM `gossip_menu_option` WHERE `MenuID` = {id};
INSERT INTO `gossip_menu_option` (`MenuID`, `OptionID`, `OptionIcon`, `OptionText`, `OptionBroadcastTextID`, `OptionType`, `OptionNpcFlag`, `ActionMenuID`, `ActionPoiID`) VALUES
({id}, 0, 0, 'Tell me more.',        0, 1, 1, {id + 1}, 0),  -- Submenu
({id}, 1, 1, 'I want to browse your goods.', 0, 3, 128, 0, 0),  -- Vendor
({id}, 2, 3, 'Train me.',            0, 5, 16, 0, 0);  -- Trainer

-- Link gossip to creature
UPDATE `creature_template` SET `gossip_menu_id` = {id} WHERE `entry` = {id};
";

    static string NpcTrainer(uint id) => $@"-- NPC Trainer (npc_trainer)
-- Teach spells from creature entry {id}
-- Creature must have npcflag 16 (Trainer) set

UPDATE `creature_template` SET `npcflag` = `npcflag` | 16 WHERE `entry` = {id};

DELETE FROM `npc_trainer` WHERE `ID` = {id};
INSERT INTO `npc_trainer` (`ID`, `SpellID`, `MoneyCost`, `ReqSkillLine`, `ReqSkillRank`, `ReqLevel`) VALUES
({id}, 71,     100,   0,   0,  1),  -- Defensive Stance (1s, level 1)
({id}, 7384, 10000, 0,   0, 20),  -- Overpower (1g, level 20)
({id}, 845,  50000, 0,   0, 40);  -- Cleave (5g, level 40)
-- SpellID = spell.dbc entry
-- ReqSkillLine/ReqSkillRank for profession trainers (e.g., 164=Blacksmithing)
";

    static string Waypoints(uint id) => $@"-- Waypoint Patrol Path (waypoint_data)
-- Path ID {id} for creature
-- Set creature.MovementType = 2 and creature_addon.path_id = {id}

DELETE FROM `waypoint_data` WHERE `id` = {id};
INSERT INTO `waypoint_data` (`id`, `point`, `position_x`, `position_y`, `position_z`, `orientation`, `delay`, `move_type`, `action`, `action_chance`) VALUES
({id}, 1, -8949.95, -132.49, 83.53, 0, 0, 0, 0, 100),
({id}, 2, -8945.00, -128.00, 83.53, 0, 0, 0, 0, 100),
({id}, 3, -8940.00, -125.00, 83.53, 0, 5000, 0, 0, 100),  -- 5s pause at point 3
({id}, 4, -8945.00, -128.00, 83.53, 0, 0, 0, 0, 100);
-- move_type: 0=Walk, 1=Run, 2=Fly
-- delay: pause duration in ms at this point
-- action: smartai action to run (if scripted)

-- Link path to creature
-- UPDATE creature SET MovementType = 2 WHERE guid = @GUID;
-- DELETE FROM creature_addon WHERE guid = @GUID;
-- INSERT INTO creature_addon (guid, path_id) VALUES (@GUID, {id});
";

    static string SmartAI(uint id) => $@"-- SmartAI Script (smart_scripts)
-- Creature entry {id} AI events
-- Set creature_template.AIName = 'SmartAI'

UPDATE `creature_template` SET `AIName` = 'SmartAI' WHERE `entry` = {id};

DELETE FROM `smart_scripts` WHERE `entryorguid` = {id} AND `source_type` = 0;
INSERT INTO `smart_scripts` (`entryorguid`, `source_type`, `id`, `link`,
    `event_type`, `event_phase_mask`, `event_chance`, `event_flags`,
    `event_param1`, `event_param2`, `event_param3`, `event_param4`,
    `action_type`, `action_param1`, `action_param2`, `action_param3`,
    `target_type`, `target_param1`,
    `comment`) VALUES

-- On aggro: say text
({id}, 0, 0, 0,
    4, 0, 100, 0,      -- event_type 4 = AGGRO
    0, 0, 0, 0,
    1, 0, 0, 0,        -- action_type 1 = SAY (broadcast_text ID in param1)
    1, 0,              -- target_type 1 = self
    'On Aggro - Say Line 0'),

-- Every 5-8s in combat: cast spell
({id}, 0, 1, 0,
    0, 0, 100, 0,      -- event_type 0 = UPDATE_IC (in combat timer)
    5000, 8000, 5000, 8000,  -- initial 5-8s, repeat 5-8s
    11, 0, 0, 0,       -- action_type 11 = CAST (spell ID in param1)
    2, 0,              -- target_type 2 = victim
    'Every 5-8s - Cast Spell'),

-- On death: say text
({id}, 0, 2, 0,
    6, 0, 100, 0,      -- event_type 6 = DEATH
    0, 0, 0, 0,
    1, 1, 0, 0,        -- action_type 1 = SAY, param1 = text group 1
    1, 0,
    'On Death - Say Line 1');

-- Creature text (used by SAY action)
DELETE FROM `creature_text` WHERE `CreatureID` = {id};
INSERT INTO `creature_text` (`CreatureID`, `GroupID`, `ID`, `Text`, `Type`, `Probability`) VALUES
({id}, 0, 0, 'You dare challenge me?', 12, 100),  -- GroupID 0 = aggro text
({id}, 1, 0, 'This... cannot be...',   12, 100);  -- GroupID 1 = death text
-- Type 12 = MONSTER_SAY, 14 = MONSTER_YELL
";
}
