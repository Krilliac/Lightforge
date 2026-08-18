# Map Asset Parser -- Vanilla/TBC Support Patch

**Target repo:** https://github.com/stoneharry/WoW-Map-Asset-Parser
**Patch file:** `map-asset-parser-vanilla-tbc.patch`

## Summary

Extends the WoW Map Asset Parser from WotLK-only (as documented) to
explicitly support Vanilla (1.12.x) and TBC (2.4.3) ADT files.

## Why this works with zero functional changes

The Map Asset Parser reads ADTs by scanning for chunk magic bytes (`MMDX`,
`MWMO`, `MTEX`) and extracting null-terminated filename strings.  This
approach is inherently version-agnostic for pre-Cataclysm files because:

- **MVER**: All pre-Cata ADTs use version 18.  The parser never checked
  this value, so no gates to remove.
- **MMDX / MWMO / MTEX**: These string-list chunks have identical binary
  layouts across Vanilla, TBC, and WotLK.  Same magic bytes, same
  null-terminated string format.
- **MDDF / MODF**: Doodad and WMO placement entries are the same 36-byte
  and 64-byte structures respectively across all three versions.
- **M2 textures** (offset 80): The `nTextures`/`ofsTextures` fields at
  offset 80/84 in the M2 header are at the same position in Vanilla, TBC,
  and WotLK M2 files.  The hardcoded `offset = 80` is correct for all.
- **WMO textures** (MODN/MOTX/MOMT): WMO chunk layouts are also identical
  across all three versions.

The tool already handles `.MDX` to `.M2` extension remapping
(`Replace(".MDX", ".M2")`), which covers the older Vanilla naming convention.

## What the patch changes

| File | Change |
|------|--------|
| `README.md` | Updated version compatibility from "WOTLK 3.3.5 only" to Vanilla/TBC/WotLK. Added format explanation. |
| `Program.cs` | Added class-level comment documenting version support. Added startup banner line listing supported versions. Added `ValidateAdtVersion()` helper that reads the MVER chunk and logs a warning for non-v18 ADTs (e.g. Cataclysm+). This is advisory only -- it does not reject files. |

## ADT format reference

All pre-Cataclysm ADTs share these properties:
- MVER chunk at file start, version field = 18
- Monolithic file (not split into root/tex/obj like Cata+)
- 256 MCNK chunks (16x16 grid) containing terrain data
- MMDX: null-terminated M2 model filename list
- MWMO: null-terminated WMO filename list
- MTEX: null-terminated texture (BLP) filename list

The only ADT differences between versions are optional sub-chunks inside
MCNK (like MCCV vertex colors in WotLK) and top-level chunks (MFBO flight
bounds, MH2O water in WotLK).  None of these affect the asset name
extraction that this tool performs.

## Risk assessment

**Very low.**  No functional code paths change.  The MVER validation is
purely advisory (logs a warning, does not block).  The tool's chunk-scanning
approach (`FindChunkOffset`) already worked with Vanilla/TBC ADTs -- this
patch just documents it and adds a safety check for post-Cata files.

## How to apply

```bash
cd WoW-Map-Asset-Parser
git apply map-asset-parser-vanilla-tbc.patch
```
