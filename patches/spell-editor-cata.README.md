# WoW Spell Editor - Cataclysm 4.3.4 Support Patch

## Overview

This patch adds initial Cataclysm 4.3.4 (build 15595) support to [stoneharry/WoW-Spell-Editor](https://github.com/stoneharry/WoW-Spell-Editor). The tool currently supports Vanilla (1.12.1), TBC (2.4.3), and WotLK (3.3.5a).

**Important**: Cataclysm fundamentally changed how spell data is stored. This patch provides the binding file infrastructure and version registration, but full UI support requires additional code changes documented below.

## What Changed in Cataclysm

### The Spell Table Split

In WotLK, `Spell.dbc` was a single monolithic table with ~282 columns containing all spell data inline. In Cataclysm, Blizzard split it into 17+ tables:

| Table | Columns | What It Contains |
|-------|---------|-----------------|
| **Spell.dbc** | 48 | Core identity: attributes, timing indices, visual IDs, name/rank/desc strings, foreign keys to sub-tables |
| **SpellEffect.dbc** | 26 | Per-effect data (up to 3 rows per spell): effect type, base points, aura, targets, triggers, class masks |
| **SpellAuraOptions.dbc** | 5 | Stack amount, proc chance/charges/flags |
| **SpellAuraRestrictions.dbc** | 9 | Caster/target aura state and spell requirements |
| **SpellCastingRequirements.dbc** | 7 | Facing flags, faction/reputation requirements, spell focus, area |
| **SpellCategories.dbc** | 7 | Category, dispel type, mechanic, damage class, prevention type |
| **SpellClassOptions.dbc** | 7 | Spell family name/flags, modal next spell, description variable |
| **SpellCooldowns.dbc** | 4 | Recovery time, category recovery, start recovery |
| **SpellEquippedItems.dbc** | 4 | Required item class/subclass/inventory type |
| **SpellInterrupts.dbc** | 6 | Interrupt flags, aura interrupt flags, channel interrupt flags (now 2 each) |
| **SpellLevels.dbc** | 4 | Base/max/spell level |
| **SpellPower.dbc** | 8 | Mana cost, cost per level, percentage, per second |
| **SpellReagents.dbc** | 17 | 8 reagent item IDs + 8 counts |
| **SpellScaling.dbc** | 16 | NEW - spell scaling coefficients, variance, combo point scaling |
| **SpellShapeshift.dbc** | 6 | Stance/shapeshift masks (now 2x uint32 each for 64-bit masks) |
| **SpellTargetRestrictions.dbc** | 6 | Targets, creature type, max targets/level, cone angle |
| **SpellTotems.dbc** | 5 | Totem and totem category requirements |

### Locale Changes

WotLK stored strings with per-locale arrays (8-16 string offsets + flag columns per text field). Cataclysm uses a single string offset per field; the client handles locale selection internally. This dramatically shrinks tables with localized text.

- WotLK `SpellName`: 9 columns (SpellName0-8) + 8 flag columns = 17 columns
- Cata `SpellName`: 1 column (SpellName0) = 1 column

### Relationship Model

WotLK: All data inline in one row per spell.

Cataclysm: Spell.dbc holds foreign keys (`AuraOptionsID`, `CategoriesID`, `CooldownsID`, etc.) pointing to rows in the sub-tables. **Exception**: SpellEffect.dbc uses a *reverse* foreign key -- each SpellEffect row has a `SpellID` column pointing back to Spell.dbc, with up to 3 rows per spell (one per effect slot).

## What This Patch Provides

### 1. Binding Files (Documentation/Bindings_434_cata/)

Complete DBC structure definitions for all Cata spell-related tables:

**New sub-tables** (17 files): Spell.txt, SpellEffect.txt, SpellAuraOptions.txt, SpellAuraRestrictions.txt, SpellCastingRequirements.txt, SpellCategories.txt, SpellClassOptions.txt, SpellCooldowns.txt, SpellEquippedItems.txt, SpellInterrupts.txt, SpellLevels.txt, SpellPower.txt, SpellReagents.txt, SpellScaling.txt, SpellShapeshift.txt, SpellTargetRestrictions.txt, SpellTotems.txt

**Auxiliary lookup tables** (14 files): SpellCastTimes.txt, SpellCategory.txt, SpellDescriptionVariables.txt, SpellDifficulty.txt, SpellDispelType.txt, SpellDuration.txt, SpellFocusObject.txt, SpellIcon.txt, SpellMechanic.txt, SpellRadius.txt, SpellRange.txt, SpellRuneCost.txt, SpellShapeshiftForm.txt, AnimationData.txt, AreaGroup.txt, AreaTable.txt, CreatureType.txt, ItemClass.txt, TotemCategory.txt

Binding file format reference: `<type> <name> [string]` where type is `uint`, `int`, or `float`, and `string` marks STRING_OFFSET columns.

### 2. Version Registration (WoWVersionManager.cs)

Adds Cataclysm as a selectable version:
- Identity: 434
- Name: "WoW 4.3.4 15595"
- Version: "4.3.4 15595"
- NumLocales: 1 (single-locale strings)
- Adds `IsCataOrGreaterSelected` static property
- Adds KeyResource for Cata visual kits (same kit/effect keys as WotLK)

### 3. Field Derivation Sources

The binding files were derived from:
- [The-Cataclysm-Preservation-Project/TrinityCore](https://github.com/The-Cataclysm-Preservation-Project/TrinityCore) master branch
- `src/server/game/DataStores/DBCStructure.h` -- C++ struct definitions
- `src/server/game/DataStores/DBCfmt.h` -- DBC format strings

Format string key: `n`=indexed uint32, `i`=int32/uint32, `f`=float, `s`=string offset, `x`=skipped by TC (column exists in DBC but TC doesn't load it), `d`=index ID (excluded from struct).

Columns marked `x` by TrinityCore are included in the binding files because the Spell Editor needs to read/write complete DBC records. Their names are best-guess based on WotLK equivalents and position in the file.

## What Full Cata Support Still Requires

The binding files let the DBC reader parse Cata files correctly, but the editor's UI and data flow assume WotLK's single-table layout. These code changes are needed:

### Priority 1: Multi-Table Import/Export

**File**: `SpellGUIV2/Sources/DBC/SpellDBC.cs`

Currently, `ImportToSql` imports a single DBC into a single SQL table named "spell". For Cata, this needs to also import all sub-table DBCs into their own SQL tables:

```csharp
// Pseudocode for what's needed:
public Task ImportToSql_Cata(IDatabaseAdapter adapter, ...)
{
    // 1. Import Spell.dbc -> spell table
    await ImportTo(adapter, ..., "ID", "Spell", _type);

    // 2. Import each sub-table
    var subTables = new[] {
        "SpellEffect", "SpellAuraOptions", "SpellAuraRestrictions",
        "SpellCastingRequirements", "SpellCategories", "SpellClassOptions",
        "SpellCooldowns", "SpellEquippedItems", "SpellInterrupts",
        "SpellLevels", "SpellPower", "SpellReagents", "SpellScaling",
        "SpellShapeshift", "SpellTargetRestrictions", "SpellTotems"
    };
    foreach (var table in subTables)
    {
        var dbc = new GenericDbc(Config.DbcDirectory + "\\" + table + ".dbc");
        await dbc.ImportTo(adapter, ..., "ID", table, _type);
    }
}
```

The export path (`Export()`) needs the reverse: join SQL tables back into individual DBC files.

### Priority 2: Flattened Spell View for the UI

**File**: `SpellGUIV2/MainWindow.xaml.cs` (~276KB, the largest change)

The UI reads/writes fields by name from a single DataRow. For Cata, the simplest approach is a SQL VIEW or joined query that flattens sub-tables into one virtual row with WotLK-compatible names:

```sql
-- Pseudocode: flattened view for Cata
CREATE VIEW spell_flat AS
SELECT
    s.ID, s.Attributes, s.AttributesEx, ...
    -- From SpellCategories (joined by s.CategoriesID)
    sc.Category, sc.DispelType AS Dispel, sc.Mechanic,
    sc.DefenseType AS DamageClass, sc.PreventionType,
    -- From SpellLevels (joined by s.LevelsID)
    sl.BaseLevel, sl.MaximumLevel AS MaximumLevel, sl.SpellLevel,
    -- From SpellCooldowns (joined by s.CooldownsID)
    scd.RecoveryTime, scd.CategoryRecoveryTime, scd.StartRecoveryTime,
    -- From SpellPower (joined by s.PowerID)
    sp.ManaCost, sp.ManaCostPerLevel, sp.ManaPerSecond,
    -- Effects are the hardest: 3 rows -> 3 sets of columns
    e1.Effect AS Effect1, e1.EffectBasePoints AS EffectBasePoints1, ...
    e2.Effect AS Effect2, e2.EffectBasePoints AS EffectBasePoints2, ...
    e3.Effect AS Effect3, e3.EffectBasePoints AS EffectBasePoints3, ...
FROM spell s
LEFT JOIN spellcategories sc ON s.CategoriesID = sc.ID
LEFT JOIN spelllevels sl ON s.LevelsID = sl.ID
LEFT JOIN spellcooldowns scd ON s.CooldownsID = scd.ID
LEFT JOIN spellpower sp ON s.PowerID = sp.ID
LEFT JOIN spelleffect e1 ON e1.SpellID = s.ID AND e1.EffectIndex = 0
LEFT JOIN spelleffect e2 ON e2.SpellID = s.ID AND e2.EffectIndex = 1
LEFT JOIN spelleffect e3 ON e3.SpellID = s.ID AND e3.EffectIndex = 2
...
```

This approach lets the existing UI code work with minimal changes -- field names like `Effect1`, `EffectBasePoints1` etc. resolve through the view. The save path would need to decompose the flat row back into updates to the individual tables.

### Priority 3: Version-Conditional UI Elements

**File**: `SpellGUIV2/MainWindow.xaml.cs`

The existing code uses `IsWotlkOrGreaterSelected` and `IsTbcOrGreaterSelected` to show/hide version-specific UI elements. Cata needs similar gating:

```csharp
// New Cata-specific fields
if (WoWVersionManager.IsCataOrGreaterSelected)
{
    // Show: AttributesEx8, AttributesEx9, AttributesEx10
    // Show: SpellScaling controls
    // Show: BonusCoefficient
    // Show: RequiredProjectID
    // Hide/change: locale string tabs (only 1 locale in Cata)
}

// Fields that changed meaning
if (WoWVersionManager.IsCataOrGreaterSelected)
{
    // EffectAmplitude is now float (was uint in WotLK)
    // EffectAuraPeriod replaces old EffectAmplitude meaning
    // AuraInterruptFlags expanded to 2 x uint32
    // ChannelInterruptFlags expanded to 2 x uint32
    // Stances/StancesNot expanded to 2 x uint32 (64-bit masks)
}
```

### Priority 4: DBCManager Sub-Table Loading

**File**: `SpellGUIV2/Sources/DBC/DBCManager.cs`

Add Cata sub-table loading in `LoadRequiredDbcs()`:

```csharp
if (WoWVersionManager.IsCataOrGreaterSelected)
{
    var subTables = new[] {
        "SpellEffect", "SpellAuraOptions", "SpellAuraRestrictions",
        "SpellCastingRequirements", "SpellCategories", "SpellClassOptions",
        "SpellCooldowns", "SpellEquippedItems", "SpellInterrupts",
        "SpellLevels", "SpellPower", "SpellReagents", "SpellScaling",
        "SpellShapeshift", "SpellTargetRestrictions", "SpellTotems"
    };
    foreach (var table in subTables)
    {
        tasks.Add(Task.Run(() => {
            var path = Config.Config.DbcDirectory + "\\" + table + ".dbc";
            if (File.Exists(path))
                InjectLoadedDbc(table, new GenericDbc(path));
        }));
    }
}
```

### Priority 5: Config Defaults

**File**: `SpellGUIV2/Sources/Config/Config.cs`

Update `ReadConfigFile()` to recognize Cata directories:

```csharp
// When version is set to Cata but directories still point to WotLK
if (WoWVersion.StartsWith("4.3.4") &&
    BindingsDirectory.Contains("335_wotlk"))
{
    BindingsDirectory = Environment.CurrentDirectory + "\\Bindings_434_cata";
    DbcDirectory = Environment.CurrentDirectory + "\\DBC_434_cata";
}
```

## WotLK-to-Cata Field Name Mapping

This table maps WotLK Spell.dbc field names (used in the UI code) to their Cata equivalents:

| WotLK Field | Cata Table | Cata Field | Notes |
|---|---|---|---|
| ID | Spell | ID | Same |
| Category | SpellCategories | Category | Via CategoriesID FK |
| Dispel | SpellCategories | DispelType | Renamed |
| Mechanic | SpellCategories | Mechanic | Via CategoriesID FK |
| Attributes | Spell | Attributes | Same |
| AttributesEx-Ex7 | Spell | AttributesEx-Ex7 | Same |
| (new) | Spell | AttributesEx8-Ex10 | New in Cata |
| Stances | SpellShapeshift | Stances1 + Stances2 | Now 64-bit (2 x uint32) |
| StancesNot | SpellShapeshift | StancesNot1 + StancesNot2 | Now 64-bit |
| Targets | SpellTargetRestrictions | Targets | Via TargetRestrictionsID FK |
| TargetCreatureType | SpellTargetRestrictions | TargetCreatureType | Via FK |
| RequiresSpellFocus | SpellCastingRequirements | RequiresSpellFocus | Via CastingRequirementsID FK |
| FacingCasterFlags | SpellCastingRequirements | FacingCasterFlags | Via FK |
| CasterAuraState | SpellAuraRestrictions | CasterAuraState | Via AuraRestrictionsID FK |
| TargetAuraState | SpellAuraRestrictions | TargetAuraState | Via FK |
| CasterAuraSpell | SpellAuraRestrictions | CasterAuraSpell | Via FK |
| TargetAuraSpell | SpellAuraRestrictions | TargetAuraSpell | Via FK |
| ExcludeCasterAuraSpell | SpellAuraRestrictions | ExcludeCasterAuraSpell | Via FK |
| ExcludeTargetAuraSpell | SpellAuraRestrictions | ExcludeTargetAuraSpell | Via FK |
| CastingTimeIndex | Spell | CastingTimeIndex | Same |
| RecoveryTime | SpellCooldowns | RecoveryTime | Via CooldownsID FK |
| CategoryRecoveryTime | SpellCooldowns | CategoryRecoveryTime | Via FK |
| InterruptFlags | SpellInterrupts | InterruptFlags | Via InterruptsID FK |
| AuraInterruptFlags | SpellInterrupts | AuraInterruptFlags1+2 | Now 64-bit |
| ChannelInterruptFlags | SpellInterrupts | ChannelInterruptFlags1+2 | Now 64-bit |
| ProcFlags | SpellAuraOptions | ProcTypeMask | Renamed, via AuraOptionsID FK |
| ProcChance | SpellAuraOptions | ProcChance | Via FK |
| ProcCharges | SpellAuraOptions | ProcCharges | Via FK |
| MaximumLevel | SpellLevels | MaximumLevel | Via LevelsID FK |
| BaseLevel | SpellLevels | BaseLevel | Via FK |
| SpellLevel | SpellLevels | SpellLevel | Via FK |
| DurationIndex | Spell | DurationIndex | Same |
| PowerType | Spell | PowerType | Same |
| ManaCost | SpellPower | ManaCost | Via PowerID FK |
| ManaCostPerLevel | SpellPower | ManaCostPerLevel | Via FK |
| ManaPerSecond | SpellPower | ManaPerSecond | Via FK |
| ManaCostPercentage | SpellPower | ManaCostPercentage | Via FK |
| RangeIndex | Spell | RangeIndex | Same |
| Speed | Spell | Speed | Same |
| ModalNextSpell | SpellClassOptions | ModalNextSpell | Via ClassOptionsID FK |
| StackAmount | SpellAuraOptions | CumulativeAura | Renamed, via AuraOptionsID FK |
| Totem1/2 | SpellTotems | Totem1/2 | Via TotemsID FK |
| TotemCategory1/2 | SpellTotems | TotemCategory1/2 | Via TotemsID FK |
| Reagent1-8 | SpellReagents | Reagent1-8 | Via ReagentsID FK |
| ReagentCount1-8 | SpellReagents | ReagentCount1-8 | Via FK |
| EquippedItemClass | SpellEquippedItems | EquippedItemClass | Via EquippedItemsID FK |
| EquippedItemSubClassMask | SpellEquippedItems | EquippedItemSubClassMask | Via FK |
| EquippedItemInventoryTypeMask | SpellEquippedItems | EquippedItemInventoryTypeMask | Via FK |
| Effect1/2/3 | SpellEffect | Effect | Via SpellID reverse FK + EffectIndex |
| EffectDieSides1/2/3 | SpellEffect | EffectDieSides | Via reverse FK |
| EffectRealPointsPerLevel1/2/3 | SpellEffect | EffectRealPointsPerLevel | Via reverse FK |
| EffectBasePoints1/2/3 | SpellEffect | EffectBasePoints | Via reverse FK |
| EffectMechanic1/2/3 | SpellEffect | EffectMechanic | Via reverse FK |
| EffectImplicitTargetA1/2/3 | SpellEffect | EffectImplicitTargetA | Via reverse FK |
| EffectImplicitTargetB1/2/3 | SpellEffect | EffectImplicitTargetB | Via reverse FK |
| EffectRadiusIndex1/2/3 | SpellEffect | EffectRadiusIndex | Via reverse FK |
| EffectApplyAuraName1/2/3 | SpellEffect | EffectAura | Renamed, via reverse FK |
| EffectAmplitude1/2/3 | SpellEffect | EffectAuraPeriod | Renamed (tick interval), via reverse FK |
| (new) | SpellEffect | EffectAmplitude | New float field (amplitude multiplier) |
| EffectMultipleValue1/2/3 | -- | -- | Removed in Cata |
| EffectChainTarget1/2/3 | SpellEffect | EffectChainTargets | Via reverse FK |
| EffectItemType1/2/3 | SpellEffect | EffectItemType | Via reverse FK |
| EffectMiscValue1/2/3 | SpellEffect | EffectMiscValue | Via reverse FK |
| EffectMiscValueB1/2/3 | SpellEffect | EffectMiscValueB | Via reverse FK |
| EffectTriggerSpell1/2/3 | SpellEffect | EffectTriggerSpell | Via reverse FK |
| EffectPointsPerComboPoint1/2/3 | SpellEffect | EffectPointsPerResource | Renamed, via reverse FK |
| EffectSpellClassMaskA/B/C 1/2/3 | SpellEffect | EffectSpellClassMaskA/B/C | Via reverse FK |
| SpellVisual1/2 | Spell | SpellVisual1/2 | Same |
| SpellIconID | Spell | SpellIconID | Same |
| ActiveIconID | Spell | ActiveIconID | Same |
| SpellName0-8 | Spell | SpellName0 | Reduced to single locale |
| SpellRank0-8 | Spell | SpellRank0 | Reduced to single locale |
| SpellDescription0-8 | Spell | SpellDescription0 | Reduced to single locale (col 23, TC skips) |
| SpellToolTip0-8 | Spell | SpellToolTip0 | Reduced to single locale (col 24, TC skips) |
| SpellFamilyName | SpellClassOptions | SpellFamilyName | Via ClassOptionsID FK |
| SpellFamilyFlags/1/2 | SpellClassOptions | SpellFamilyFlags/1/2 | Via FK |
| MaximumAffectedTargets | SpellTargetRestrictions | MaximumAffectedTargets | Via FK |
| DamageClass | SpellCategories | DefenseType | Renamed, via FK |
| PreventionType | SpellCategories | PreventionType | Via FK |
| StartRecoveryCategory | SpellCategories | StartRecoveryCategory | Via FK |
| StartRecoveryTime | SpellCooldowns | StartRecoveryTime | Via FK |
| MaximumTargetLevel | SpellTargetRestrictions | MaximumTargetLevel | Via FK |
| EffectDamageMultiplier1/2/3 | -- | -- | Merged into SpellEffect.EffectBonusCoefficient |
| EffectBonusMultiplier1/2/3 | Spell | BonusCoefficient | Single float, was per-effect |
| MinimumFactionId | SpellCastingRequirements | MinimumFactionId | Via FK |
| MinimumReputation | SpellCastingRequirements | MinimumReputation | Via FK |
| RequiredAuraVision | SpellCastingRequirements | RequiredAuraVision | Via FK |
| AreaGroupID | SpellCastingRequirements | RequiredAreasID | Renamed, via FK |
| SchoolMask | Spell | SchoolMask | Same |
| RuneCostID | Spell | RuneCostID | Same |
| SpellMissileID | Spell | SpellMissileID | Same (col 27, TC skips) |
| PowerDisplayId | Spell | PowerDisplayId | Same (col 28, TC skips) |
| SpellDescriptionVariableID | SpellClassOptions | SpellDescriptionVariableID | Moved to sub-table |
| SpellDifficultyID | Spell | Difficulty | Renamed |
| StanceBarOrder | -- | -- | Removed in Cata |
| (new) | SpellEffect | EffectRadiusMaxIndex | New in Cata |
| (new) | SpellEffect | EffectBonusCoefficient | New in Cata |
| (new) | SpellEffect | EffectChainAmplitude | New in Cata |
| (new) | SpellTargetRestrictions | ConeAngle | New in Cata (float) |
| (new) | SpellScaling | (all fields) | Entirely new table |
| (new) | Spell | AttributesEx8-10 | 3 new attribute flags |
| (new) | Spell | BonusCoefficient | New float |
| (new) | Spell | RequiredProjectID | New |

## Known Limitations and Caveats

1. **Column 23/24 in Spell.dbc**: Labeled as SpellDescription0 and SpellToolTip0 (string offsets). TrinityCore marks these as 'x' (skipped) because the server doesn't need client display text. These bindings include them as string columns. If the actual Cata DBC uses these positions for something else, the binding will need correction. Test with real 4.3.4 client DBC files.

2. **Columns 27, 28, 38 in Spell.dbc**: Best-guess names (SpellMissileID, PowerDisplayId, Unknown38). Verify against actual files.

3. **SpellShapeshiftForm.txt**: The Cata format string `nxxiixiiiiiiiiiiiiiiiix` indicates 21 columns, but exact field names for all positions are uncertain. The binding provides best-guess names.

4. **Auxiliary tables with locale strings**: Tables like SpellDispelType, SpellMechanic, SpellFocusObject, CreatureType, AreaTable, SpellRange changed their locale handling (from per-locale arrays to single locale). The bindings assume single-locale format. If any of these tables retained multi-locale format in 4.3.4, the binding will fail record-size validation.

5. **SpellVisual.dbc**: Not included in this patch. The Cata format (`dxxxxxxiixxxxxxxxxxxxxxxxxxxxxxxi`) has many skipped columns. A dedicated effort is needed to map the Cata SpellVisual structure, or the WotLK SpellVisual binding can be tested to see if the DBC header matches.

6. **DBC vs DB2 format**: All spell-related tables in 4.3.4 are believed to use classic DBC format ('WDBC' magic). If any turn out to be DB2 format ('WDB2' magic), the existing `DBCReader.cs` will not be able to read them and will need a DB2 reader implementation.

7. **SpellEffect relationship**: SpellEffect.dbc uses a *reverse* foreign key (`SpellID` in SpellEffect points to `ID` in Spell). The existing import flow assumes 1:1 ID-based lookup. Loading effects for a given spell requires scanning/indexing SpellEffect by SpellID.

## Estimated Implementation Effort

| Component | Effort | Notes |
|-----------|--------|-------|
| Binding files (this patch) | Done | Ready to parse Cata DBC files |
| Version registration (this patch) | Done | Cata selectable in config |
| Multi-table import | Medium | New method in SpellDBC.cs, ~100 lines |
| Multi-table export | Medium | Reverse of import, ~150 lines |
| SQL flattened view | Medium | Join query + decompose on save, ~200 lines |
| MainWindow version guards | Large | 15+ conditional blocks in 276KB file |
| New Cata-specific UI controls | Large | AttributesEx8-10, scaling, bonus coefficient |
| Locale handling for NumLocales=1 | Small | Already mostly handled by existing code |
| Testing with real DBC files | Critical | Validates all binding field counts and record sizes |

Total estimate: ~2-3 weeks of focused development for a developer familiar with the codebase.

## Testing

1. Extract DBC files from a 4.3.4 WoW client (using the included MPQ export or an external tool)
2. Set BindingsDirectory to `Bindings_434_cata`
3. Set DbcDirectory to the extracted Cata DBC folder
4. Select "4.3.4 15595" as the version
5. Attempt to import Spell.dbc -- if the record size/field count doesn't match, adjust the binding file
6. Import each sub-table individually through the Import/Export window to verify bindings
