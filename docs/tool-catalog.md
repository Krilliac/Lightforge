# Tool Catalog

Complete reference for all 63 tools registered in Lightforge, organized by category. Each entry includes the tool name, description, supported WoW versions, source repository, and current maintenance status.

## Version Key

| Abbreviation | Versions |
|-------------|----------|
| Classic Trio | Vanilla 1.12.1, TBC 2.4.3, WotLK 3.3.5a |
| All MPQ | Vanilla through MoP (1.x - 5.x) |
| All CASC | WoD through BfA (6.x - 8.x) |
| All | Every supported version |

## Status Key

| Status | Meaning |
|--------|---------|
| **Active** | Actively maintained, recent commits |
| **Maintained** | Maintained but not frequently updated |
| **Stable** | Feature-complete, no active development needed |
| **Stale** | No recent activity, may still work |
| **Dormant** | Abandoned but functional |
| **Archived** | Read-only archive, no further development |
| **Complete** | Finished tool, does its job |

---

## World Editing (7 tools)

### Noggit3
- **Description:** 3D terrain and map editor for WotLK
- **Versions:** WotLK
- **Source:** https://github.com/wowdev/noggit3
- **Status:** Active
- **Notes:** The original community map editor. Edits ADT terrain, textures, and object placement.

### Noggit RED
- **Description:** Enhanced fork of Noggit3 with modern features
- **Versions:** WotLK
- **Source:** https://gitlab.com/varenroth/noggit-red
- **Status:** Active
- **Notes:** Adds undo/redo, improved UI, and better performance over the original.

### WoW Blender Studio
- **Description:** Blender addon for WoW world editing
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://gitlab.com/skarnproject/blender-wow-studio
- **Status:** Active
- **Notes:** Full WMO/ADT editing pipeline inside Blender. Import, modify, and export WoW assets.

### ADT Creator
- **Description:** Generates blank ADT terrain files
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://github.com/tswow/adt-creator
- **Status:** Active
- **Notes:** Creates new map tiles from scratch. Useful for custom continents.

### AdtTools
- **Description:** ADT terrain manipulation utilities
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://github.com/kelno/AdtTools
- **Status:** Maintained
- **Notes:** Reads and modifies ADT chunk data. Vanilla support added via patch (MCCV field handling).

### MapUpconverter
- **Description:** Converts WotLK maps to modern client format
- **Versions:** WotLK, Legion, BfA
- **Source:** https://github.com/ModernWoWTools/MapUpconverter
- **Status:** Active
- **Notes:** Takes WotLK-format ADTs and outputs Legion/BfA-compatible files for modern client modding.

### Neo
- **Description:** Map viewer and editor for modern WoW formats
- **Versions:** WotLK, WoD
- **Source:** https://github.com/WowDevTools/Neo
- **Status:** Maintained
- **Notes:** Cross-version map viewer with editing capabilities.

---

## Data Editing (9 tools)

### WDBXEditor
- **Description:** DBC/DB2 database editor supporting all formats
- **Versions:** All
- **Source:** https://github.com/WowDevTools/WDBXEditor
- **Status:** Active
- **Notes:** The standard tool for editing game database files. Handles DBC (pre-Cata), DB2 (WDB2-WDB6), and all variants.

### WDBXEditor2
- **Description:** Modern DB2 editor for Cata+ formats
- **Versions:** Cata, MoP, WoD, Legion, BfA
- **Source:** https://github.com/MaxtorCoder/WDBXEditor2
- **Status:** Active
- **Notes:** Focused on modern DB2 format variants. Cleaner UI than the original.

### Spell Editor V2
- **Description:** Visual spell DBC editor
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://github.com/stoneharry/WoW-Spell-Editor
- **Status:** Active
- **Notes:** Purpose-built UI for Spell.dbc editing. Understands spell field relationships and effects.

### SpellWork
- **Description:** Spell database browser and analyzer
- **Versions:** All
- **Source:** https://github.com/TrinityCore/SpellWork
- **Status:** Active
- **Notes:** From the TrinityCore project. Browses spell data with filter and search.

### WoW Database Editor
- **Description:** Visual database editor with SmartScript support
- **Versions:** WotLK, Cata
- **Source:** https://github.com/BAndysc/WoWDatabaseEditor
- **Status:** Active
- **Notes:** Advanced database editing with visual SmartAI script editor, conditions editor, and SQL generation.

### Keira3
- **Description:** Database editor for AzerothCore servers
- **Versions:** WotLK
- **Source:** https://github.com/azerothcore/Keira3
- **Status:** Active
- **Notes:** Electron-based editor tightly integrated with AzerothCore's database schema.

### TrinityCreator
- **Description:** Item/creature/quest creator for TrinityCore
- **Versions:** WotLK
- **Source:** https://github.com/NotCoffee418/TrinityCreator
- **Status:** Maintained
- **Notes:** Generates SQL insert statements for TrinityCore databases with a form-based UI.

### AoWoW
- **Description:** WoW database website engine (like Wowhead)
- **Versions:** WotLK
- **Source:** https://github.com/Sarjuuk/aowow
- **Status:** Active
- **Notes:** PHP-based database browser. Self-hosted Wowhead-style site for your server.

### SAI-Editor
- **Description:** SmartAI event script editor
- **Versions:** WotLK, Cata
- **Source:** https://github.com/jasper-rietrae2/SAI-Editor
- **Status:** Maintained
- **Notes:** Visual editor for TrinityCore SmartAI scripts with event/action/target builder.

---

## Model Tools (6 tools)

### M2Mod
- **Description:** M2 model modifier and character customization
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://github.com/M2Mod/m2mod
- **Status:** Maintained
- **Notes:** Imports/exports M2 models for modification. Primary tool for character model editing.

### MultiConverter
- **Description:** Batch model/texture format converter
- **Versions:** All
- **Source:** https://github.com/MaxtorCoder/MultiConverter
- **Status:** Archived
- **Notes:** Converts between M2/BLP format versions. Archived but still functional.

### BLPConverter
- **Description:** BLP texture format converter
- **Versions:** All
- **Source:** https://github.com/Kanma/BLPConverter
- **Status:** Stable
- **Notes:** Converts between BLP (WoW texture format) and standard image formats (PNG, TGA, etc.).

### BLP Lab
- **Description:** Advanced BLP texture editor with preview
- **Versions:** All
- **Source:** Closed source
- **Status:** Active
- **Notes:** GUI tool for BLP editing with real-time preview and batch operations.

### jM2converter
- **Description:** Java-based M2 model converter
- **Versions:** All
- **Source:** https://github.com/WowDevs/jM2converter
- **Status:** Stale
- **Notes:** Cross-platform M2 converter. No recent development but handles basic conversions.

### Map Asset Parser
- **Description:** Extracts model/object placements from ADT files
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://github.com/stoneharry/WoW-Map-Asset-Parser
- **Status:** Maintained
- **Notes:** Parses MDDF/MODF chunks to list all doodads and WMOs placed on a map tile.

---

## Viewers (4 tools)

### WMVx
- **Description:** WoW Model Viewer (modern rewrite)
- **Versions:** All
- **Source:** https://github.com/Frostshake/WMVx
- **Status:** Active
- **Notes:** Complete rewrite of the classic WoW Model Viewer. Supports all M2/WMO versions.

### Everlook
- **Description:** Cross-platform WoW asset viewer
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** https://github.com/WowDevTools/Everlook
- **Status:** Maintained
- **Notes:** OpenGL-based viewer for models, textures, and maps. Linux/macOS/Windows.

### wow.export
- **Description:** WoW asset extraction and preview tool
- **Versions:** All
- **Source:** https://github.com/Kruithne/wow.export
- **Status:** Active
- **Notes:** Extracts and previews WoW assets. Exports to OBJ/glTF for use in 3D software.

### wow.tools.local
- **Description:** Local version of wow.tools database browser
- **Versions:** All
- **Source:** https://github.com/Marlamin/wow.tools.local
- **Status:** Active
- **Notes:** Self-hosted wow.tools instance for browsing game data files locally.

---

## Generators (3 tools)

### GObject Spawner
- **Description:** SQL generator for gameobject spawns
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** Bundled
- **Status:** Stable
- **Notes:** Generates SQL INSERT statements for the gameobject table. Works across all versions using the same table schema.

### Minimap Gen
- **Description:** Generates minimap tiles from ADT heightmaps
- **Versions:** Vanilla, TBC, WotLK
- **Source:** Bundled
- **Status:** Stable
- **Notes:** Creates minimap BLP files from terrain data.

### WoWHeightGen
- **Description:** Heightmap generator from real-world elevation data
- **Versions:** All
- **Source:** https://github.com/CucFlavius/WoWHeightGen
- **Status:** Active
- **Notes:** Imports real-world terrain data (SRTM, etc.) into WoW-format heightmaps.

---

## Packaging (4 tools)

### MPQ Editor
- **Description:** MPQ archive editor (Ladislav Zezula)
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** http://www.zezula.net/en/mpq/download.html
- **Status:** Active
- **Notes:** The standard MPQ archive tool. Create, edit, and extract MPQ files.

### mpqcli
- **Description:** Command-line MPQ tool
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** https://github.com/TheGrayDot/mpqcli
- **Status:** Active
- **Notes:** CLI alternative to MPQ Editor for scripted/batch operations.

### CASCExplorer
- **Description:** CASC archive browser and extractor
- **Versions:** WoD, Legion, BfA
- **Source:** https://github.com/WoW-Tools/CASCExplorer
- **Status:** Active
- **Notes:** Browse and extract files from CASC archives used by WoD+ clients.

### CASCHost
- **Description:** Local CASC content server
- **Versions:** WoD, Legion, BfA
- **Source:** https://github.com/WowDevTools/CASCHost
- **Status:** Active
- **Notes:** Hosts modified CASC content locally for development and testing.

---

## Network (3 tools)

### WowPacketParser
- **Description:** Network packet parser and analyzer
- **Versions:** All
- **Source:** https://github.com/TrinityCore/WowPacketParser
- **Status:** Active
- **Notes:** Parses WoW network packet captures into human-readable format. From TrinityCore.

### ymir
- **Description:** Network proxy for WoW packet inspection
- **Versions:** All
- **Source:** https://github.com/TrinityCore/ymir
- **Status:** Active
- **Notes:** Man-in-the-middle proxy that captures live packet traffic between client and server.

### SzimatSzatyor
- **Description:** In-process packet sniffer via DLL injection
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** https://github.com/Anubisss/SzimatSzatyor
- **Status:** Dormant
- **Notes:** Injects into the WoW process to capture packets. DLL injection approach works through 5.x.

---

## Client Patching (5 tools)

### RCE Patcher
- **Description:** Remote code execution patcher for WotLK
- **Versions:** WotLK
- **Source:** https://github.com/Gargash/WoW-RCE-Patcher
- **Status:** Complete
- **Notes:** Patches the WotLK client to fix known RCE vulnerabilities.

### Client Patcher 335
- **Description:** General-purpose WotLK client patcher
- **Versions:** WotLK
- **Source:** https://github.com/Stormhand-dev/WoW-3.3.5-Patcher---Project-Reforged
- **Status:** Active
- **Notes:** Applies various client patches: custom realmlist, widescreen fixes, etc.

### wow-patcher
- **Description:** Client patcher for modern WoW versions
- **Versions:** Legion, BfA
- **Source:** https://github.com/wowemulation-dev/wow-patcher
- **Status:** Active
- **Notes:** Patches modern WoW clients for private server use. Handles Battle.net auth bypass.

### VanillaFixes
- **Description:** Stutter fix and custom DLL loader for 1.12.1
- **Versions:** Vanilla
- **Source:** https://github.com/hannesmann/vanillafixes
- **Status:** Active
- **Notes:** Fixes frame stuttering in the Vanilla client and provides a DLL loader for other mods.

### Arctium Launcher
- **Description:** Client launcher for modern WoW private servers
- **Versions:** WoD, Legion, BfA
- **Source:** https://github.com/Arctium/WoW-Launcher
- **Status:** Active
- **Notes:** Launches WoD+ clients with server connection patches applied.

---

## Retroporting (2 tools)

### Retroporting Scripts
- **Description:** Asset conversion scripts between WoW versions
- **Versions:** All
- **Source:** https://github.com/fischerlol/retroporting
- **Status:** Maintained
- **Notes:** Collection of scripts for retroporting assets from newer WoW versions to older ones (typically to WotLK).

### GOB Retroport
- **Description:** Game object retroporting tool
- **Versions:** All
- **Source:** https://github.com/GmFactoryWoW/gob_retroport
- **Status:** Stale
- **Notes:** Specifically handles retroporting game objects between versions.

---

## Server Admin (3 tools)

### AzerothAdmin
- **Description:** Web admin panel for AzerothCore/TrinityCore
- **Versions:** WotLK
- **Source:** https://github.com/superstyro/AzerothAdmin
- **Status:** Active
- **Notes:** PHP-based admin panel for managing WotLK private servers.

### TSWoW
- **Description:** TypeScript modding framework for WotLK
- **Versions:** WotLK
- **Source:** https://github.com/tswow/tswow
- **Status:** Active
- **Notes:** Full modding framework with TypeScript scripting, custom content creation, and build pipeline.

### SPP Legion Admin
- **Description:** In-game admin control panel addon for Legion
- **Versions:** Legion
- **Source:** https://github.com/faustus1005/SPPLegionAdmin
- **Status:** Active
- **Notes:** Client addon providing admin controls for Single Player Project Legion servers.

---

## Libraries (9 tools)

### warcraft-rs
- **Description:** Rust library for WoW file formats
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** https://github.com/wowemulation-dev/warcraft-rs
- **Status:** Active
- **Notes:** Rust crate for reading MPQ archives, ADT, M2, WMO, DBC, and WDT files.

### StormLib
- **Description:** C/C++ library for MPQ archives
- **Versions:** Vanilla, TBC, WotLK, Cata, MoP
- **Source:** https://github.com/ladislav-zezula/StormLib
- **Status:** Active
- **Notes:** The reference MPQ library used by most other tools. By Ladislav Zezula.

### CASCLib
- **Description:** C/C++ library for CASC archives
- **Versions:** WoD, Legion, BfA
- **Source:** https://github.com/ladislav-zezula/CascLib
- **Status:** Active
- **Notes:** The reference CASC library. Companion to StormLib for modern WoW versions.

### libwarcraft
- **Description:** C# library for WoW file formats
- **Versions:** All
- **Source:** https://github.com/WowDevTools/libwarcraft
- **Status:** Maintained
- **Notes:** .NET library covering most WoW binary formats.

### pywowlib
- **Description:** Python library for WoW file formats
- **Versions:** All
- **Source:** https://github.com/wowdev/pywowlib
- **Status:** Active
- **Notes:** Python bindings for working with WoW assets. Used by WoW Blender Studio.

### Warcraft.NET
- **Description:** Modern .NET library for WoW file formats
- **Versions:** All
- **Source:** https://github.com/ModernWoWTools/Warcraft.NET
- **Status:** Active
- **Notes:** Actively maintained .NET library with support for modern format variants.

### namigator
- **Description:** Server-side navigation mesh library
- **Versions:** Vanilla, TBC, WotLK
- **Source:** https://github.com/namreeb/namigator
- **Status:** Active
- **Notes:** Generates and queries navigation meshes from ADT/WMO data for server pathfinding.

### DBCD
- **Description:** .NET library for DB2 file format
- **Versions:** Cata, MoP, WoD, Legion, BfA
- **Source:** https://github.com/wowdev/DBCD
- **Status:** Active
- **Notes:** Reads and writes all DB2 format variants (WDB2 through WDB6).

### WoWDBDefs
- **Description:** Community DB2 structure definitions
- **Versions:** All
- **Source:** https://github.com/wowdev/WoWDBDefs
- **Status:** Active
- **Notes:** Machine-readable definitions of every DB2 table's column layout, across all versions. Used by WDBXEditor, DBCD, and other tools.

---

## ADT CLI Tools (18 tools)

Command-line utilities for batch ADT terrain operations, originally by Cryect (2006-2009) with Schlumpf additions. All work with pre-Cataclysm ADT files (MVER=18 format: Vanilla, TBC, WotLK).

**Sources:**
- https://github.com/skarndev/mctools
- https://github.com/merfed/Coffee/tree/master/Tools/ADT

**Tools:** AddDetailDoodads, AllOcean, AllWater, CoverWithWater, CreateWDT, FileInfo, FixAllOcean, GetSlopePicture, LoadInfo, MakeMap, MapMover, ModelSwap, OffsetFix, PatchHoles, RaiseTerrain, RemoveSound, RemoveTheWalls, SetChunkFlags
