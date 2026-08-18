# Lightforge Tool Patches

Version support extensions for WoW modding tools. Each patch targets a specific
tool's source code to extend the WoW versions it supports.

## Format-compatible extensions (no code patch needed)

These tools work with more versions than originally labeled because the
underlying binary format is identical. No source code changes required.

| Tool | Was | Now | Reason |
|------|-----|-----|--------|
| ADT CLI Tools (18) | WotLK | Vanilla/TBC/WotLK | ADT format MVER=18 identical across all pre-Cata versions |
| ADT Creator | WotLK | Vanilla/TBC/WotLK | Creates blank ADTs in MVER=18 format |
| AdtTools | TBC/WotLK | Vanilla/TBC/WotLK | Author confirms "should work for 1.12"; same ADT format |
| Map Asset Parser | WotLK | Vanilla/TBC/WotLK | MDDF/MODF chunks unchanged in pre-Cata |
| Minimap Gen | WotLK | Vanilla/TBC/WotLK | Heightmap data format unchanged |
| GObject Spawner | WotLK | Vanilla–MoP | SQL templates use same gameobject table schema |
| MapUpconverter | WotLK | WotLK/Legion/BfA | Outputs modern ADT format for these targets |
| Retroporting Scripts | WotLK | All | Retroports FROM any version TO WotLK |
| GOB Retroport | WotLK | All | Retroports objects FROM any version |
| SzimatSzatyor | Vanilla–WotLK | Vanilla–MoP | DLL injection approach works through 5.x |

## Source patches (require code changes)

Each patch below has a .patch file (unified diff) and a .README.md explaining
what it changes, how to apply it, and known limitations.

| Patch file | Tool | Extension |
|------------|------|-----------|
| m2mod-cata-mop.patch | M2Mod | Vanilla–WotLK → Vanilla–MoP |
| spell-editor-cata.patch | Spell Editor | Classic trio → +Cata |
| map-asset-parser-vanilla-tbc.patch | Map Asset Parser | WotLK → +Vanilla/TBC |
| adttools-vanilla.patch | AdtTools | TBC/WotLK → +Vanilla |
| wow-patcher-cata-mop-wod.patch | wow-patcher | Legion/BfA → +Cata/MoP/WoD |
| warcraft-rs-wod.patch | warcraft-rs | Vanilla–MoP → +WoD |

## Deferred (big projects)

These tools need fundamental format support changes beyond simple patches:

- **Noggit3 / Noggit RED** — Cata+ ADT format (split root/tex/obj) is a rewrite
- **Keira3** — AzerothCore-specific DB schema, not generalizable
- **TSWoW** — Entire TypeScript framework is WotLK-coupled
- **WoW Blender Studio** — Cata+ WMO/ADT format support is in progress upstream

## How to apply patches

```bash
cd /path/to/cloned-tool-repo
git apply /path/to/lightforge/patches/tool-name.patch
```
