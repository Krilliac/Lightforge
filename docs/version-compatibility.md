# Version Compatibility Guide

How WoW's binary file formats changed across expansions, and what it means for tool compatibility.

## Archive Format Split

The most fundamental compatibility boundary is the archive format:

| Versions | Format | Library |
|----------|--------|---------|
| 1.12.1 - 5.4.8 (Vanilla - MoP) | **MPQ** | StormLib |
| 6.2.4 - 8.3.7 (WoD - BfA) | **CASC** | CASCLib |

MPQ (Mo'PaQ) archives are single files containing a hash table and block table. CASC (Content Addressable Storage Container) replaced MPQ starting with WoD and uses a content-addressed layout with encoding/root tables.

Tools that only read/write MPQ cannot access CASC archives and vice versa. Some tools (WDBXEditor, wow.export) work with extracted files and are format-agnostic.

## ADT Terrain Files

ADTs (Area Data Terrain) define the terrain, textures, and object placements for each map tile.

### Pre-Cataclysm (MVER = 18)

Vanilla 1.12.1, TBC 2.4.3, and WotLK 3.3.5a all use the same monolithic ADT format with `MVER` version 18. A single `.adt` file contains:

- `MHDR` -- Header with offsets to all other chunks
- `MCIN` -- Index of MCNK (terrain cell) positions
- `MTEX` -- Texture filename list
- `MMDX` -- M2 model filename list
- `MMID` -- M2 model filename offsets
- `MWMO` -- WMO filename list
- `MWID` -- WMO filename offsets
- `MDDF` -- M2 model placement data (position, rotation, scale)
- `MODF` -- WMO placement data
- 256x `MCNK` -- Individual terrain cells with heightmap, vertex colors, alpha maps

**Key difference between versions:** WotLK added the `MCCV` (vertex color) sub-chunk in MCNK cells, flagged by bit 0x40 in MCNK flags. Vanilla ADTs lack this chunk, but the offset field at MCNK+0x74 contains `textureId` data in Vanilla versus `ofsMCCV` in WotLK. Tools must check the flag before interpreting this field.

This format identity means any tool that works with WotLK ADTs also works with Vanilla and TBC ADTs, as long as it respects the MCCV flag.

### Cataclysm and Later

Cataclysm (4.3.4) split the monolithic ADT into three files:
- `mapname_XX_YY.adt` -- Root file (terrain cells)
- `mapname_XX_YY_tex0.adt` -- Texture data
- `mapname_XX_YY_obj0.adt` -- Object placement data

This is a fundamental format change. Tools built for monolithic ADTs cannot read split ADTs without significant rework. This is why Noggit3, WoW Blender Studio, and other world editors are limited to pre-Cata versions.

## M2 Model Files

M2 files contain character models, creatures, doodads, and other 3D objects.

| Version Range | Header Version | Notes |
|--------------|----------------|-------|
| Vanilla 1.12.1 | 256-260 | Original format |
| TBC 2.4.3 | 260-263 | Minor additions |
| WotLK 3.3.5a | 264 | Particle system changes |
| Cata 4.3.4 | 272 | MD20 magic, major restructure |
| MoP 5.4.8 | 272-274 | Extended header |
| WoD 6.2.4 | 272 | Same as Cata/MoP |
| Legion 7.3.5 | 274 | Chunked format |
| BfA 8.3.7 | 274 | Further chunked additions |

Pre-Cata M2 files use `MD20` magic with a fixed-size header. Cataclysm introduced a major header restructure while keeping the same magic. Legion introduced a chunked format where data sections are identified by four-character codes.

## DBC / DB2 Database Files

Game database tables changed format significantly:

| Version | Format | Notes |
|---------|--------|-------|
| Vanilla - WotLK | **DBC** | Fixed-size records, string table |
| Cata 4.3.4 | **WDB2** | Added min/max ID, locale bitmask |
| MoP 5.4.8 | **WDB2** | Same format as Cata |
| WoD 6.2.4 | **WDB5/WDB6** | Per-column compression, field storage info |
| Legion 7.3.5 | **WDB5/WDB6** | Same as WoD |
| BfA 8.3.7 | **WDB5/WDB6** | Same as WoD |

WDBXEditor handles all variants. The `WoWDBDefs` project provides column definitions for every table across every version.

## WMO (World Map Objects)

WMO files define large structures (buildings, dungeons, etc.) and remain relatively stable across versions. The format uses group files (`*_000.wmo`, `*_001.wmo`, etc.) alongside a root file. Key versions:

- Pre-Cata: portal, light, and doodad systems are consistent
- Cata+: additional chunks but backward-compatible reading

## Practical Implications

### What works across the Classic Trio (Vanilla/TBC/WotLK)

Most pre-Cata tools are interchangeable because the file formats share `MVER=18` for ADTs, similar M2 headers, and identical DBC structure. The main caveat is the MCCV vertex color chunk in ADTs.

### The Cataclysm Boundary

Cataclysm is the biggest compatibility break:
1. Split ADT files
2. Major M2 header restructure
3. DBC replaced by DB2

Most world editing tools cannot cross this boundary without significant effort.

### The CASC Boundary

WoD replaced MPQ with CASC. Tools that access game files through MPQ libraries (StormLib) need CASC alternatives (CASCLib) for WoD+. Many tools sidestep this by working with pre-extracted files.

### Why Version Support Varies

A tool listed as "WotLK only" might actually work with Vanilla/TBC if its underlying format assumptions hold. The Lightforge patches directory documents cases where tools were tested and confirmed to work with additional versions without code changes, and provides source patches for tools that need minor modifications.
