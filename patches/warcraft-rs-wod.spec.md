# warcraft-rs WoD (6.x) Support Extension -- Patch Specification

**Target:** https://github.com/wowemulation-dev/warcraft-rs (v0.7.0, Rust 2024 edition)
**Date:** 2026-08-17
**Scope:** Relax version restrictions where WoD binary format is already compatible;
document what needs new implementation for true 6.x support.

---

## Executive Summary

warcraft-rs currently targets Vanilla through MoP (1.x--5.x). Inspection of the
source code reveals that **most format parsers already accept WoD-era files
without modification**, because the binary structures did not change between MoP
and WoD for the majority of formats. The one fundamental blocker is the archive
layer: WoD replaced MPQ with CASC, and warcraft-rs has no CASC reader.

| Format | WoD Status | Work Required |
|--------|-----------|---------------|
| BLP    | Already works | None -- BLP2 magic is version-agnostic |
| M2     | Already works | Cosmetic: version 272 label covers Cata/MoP/WoD |
| WMO    | Already works | from_raw(18) already maps to Self::Wod |
| WDT    | Already works | WowVersion::WoD variant exists, major=6 accepted |
| ADT    | Parses as MoP | Add WoD variant + detection (low priority) |
| DBC    | Partial | WDB5 works; WDB6 needs new parser |
| MPQ    | N/A for WoD | WoD game data is in CASC, not MPQ |
| CASC   | Not implemented | Fundamental blocker for accessing WoD data |

---

## Format-by-Format Analysis

### 1. BLP Textures -- NO CHANGE NEEDED

**File:** `file-formats/graphics/wow-blp/src/convert/mod.rs`

The BLP parser identifies format by magic bytes: `BLP0`, `BLP1`, `BLP2`.
WoD textures use `BLP2` -- the same magic and binary layout as every WoW
expansion from TBC (2.x) through at least Legion (7.x). There is no version
field inside BLP2 that could act as a gate.

**Encodings supported:** Raw1 (paletted), JPEG, Raw3, DXT1/DXT3/DXT5 --
all encodings used by WoD BLP files.

**Verdict:** BLP2 WoD files parse identically to MoP BLP2 files. No patch.

---

### 2. M2 Models -- COSMETIC LABEL FIX ONLY

**Files:**
- `file-formats/graphics/wow-m2/src/version.rs`
- `file-formats/graphics/wow-m2/src/header.rs`

**Current behavior:**

The parser accepts MD20 magic and validates the header version via
`M2Version::from_header_version(version)`. The mapping:

```rust
272 => Some(Self::Cataclysm), // Cataclysm 4.3.4 and MoP 5.4.8
273..=279 => Some(Self::Legion),
```

WoD M2 files use **version 272 with MD20 magic** -- the header version did
not change between Cataclysm, MoP, and WoD. The MD21 chunked format was
introduced in Legion, not WoD.

**What already works:**
- Magic validation: MD20 accepted (WoD uses MD20)
- Version validation: 272 accepted (mapped to Cataclysm, but parses fine)
- All version-dependent field parsing (playable_animation_lookup, views vs
  num_skin_profiles, texture_flipbooks) keys off numeric ranges, not enum
  variants, so the Cataclysm label is irrelevant to parsing correctness

**What does NOT work:**
- `M2Version::from_header_version(272)` returns `Self::Cataclysm`, not
  `Self::WoD`. This is cosmetically wrong but does not affect parsing.
- The `to_header_version(Self::WoD)` returns 275, labeled "Theoretical WoD
  chunked version" -- this would be wrong for writing WoD-era M2 files.

**Proposed patch (cosmetic, optional):**

```rust
// In from_header_version:
// Change the comment on 272 to acknowledge it covers Cata/MoP/WoD:
272 => Some(Self::Cataclysm), // Cataclysm 4.3.4, MoP 5.4.8, WoD 6.x

// In to_header_version:
Self::WoD => 272, // WoD uses same header version as Cata/MoP
```

**Verdict:** WoD M2 files already parse. Optional comment/label fixes.

---

### 3. WMO World Map Objects -- ALREADY WORKS

**File:** `file-formats/graphics/wow-wmo/src/version.rs`

WMO files carry a raw version number in their MVER chunk:
- Classic through MoP: raw version 17
- WoD: raw version 18
- Legion: raw version 19
- (etc.)

The `from_raw()` function already handles this:

```rust
17 => Some(Self::Classic),
18 => Some(Self::Wod),   // <-- WoD already mapped
19 => Some(Self::Legion),
```

The parser reads MVER, calls `from_raw()`, gets `Self::Wod`, and proceeds.
Feature detection methods gate version-specific chunk parsing. WoD-specific
chunks that are unrecognized by the parser would be silently skipped (the
chunk-based parsing model tolerates unknown chunks).

**Verdict:** WoD WMO files already parse. No patch needed.

---

### 4. WDT World Data Tables -- ALREADY WORKS

**File:** `file-formats/world-data/wow-wdt/src/version.rs`

The WowVersion enum includes a `WoD` variant. `from_string("6.x.y")`
correctly maps major version 6 to `WowVersion::WoD`. The WDT format uses
the same MVER=18 across all versions; version detection relies on chunk
presence (MAID for BfA+, MWMO behavior for pre-Cata, etc.).

WoD WDT files do not have MAID (BfA+), do have terrain behavior consistent
with Cata+, and would be correctly identified and parsed.

**Verdict:** WoD WDT files already parse. No patch needed.

---

### 5. ADT Terrain -- MINOR ENHANCEMENT

**File:** `file-formats/world-data/wow-adt/src/version.rs`

**Current behavior:**

The AdtVersion enum stops at MoP:

```rust
pub enum AdtVersion {
    VanillaEarly,
    VanillaLate,
    TBC,
    WotLK,
    Cataclysm,
    MoP,
    // No WoD variant
}
```

Detection is by chunk presence. WoD ADT files would have MTXP (like MoP)
plus potentially WoD-specific chunks. The detection hierarchy:

```rust
if chunks.contains_key(&ChunkId::MTXP) {
    Self::MoP  // WoD files land here too
}
```

**What works:** WoD ADT files parse fine -- they are detected as MoP, and
the parser reads all MoP-era chunks. Unknown WoD-specific chunks are skipped.

**What could be improved:** Adding a WoD variant with detection based on
WoD-specific chunks (if any exist in the chunk_id.rs definitions). The
blend mesh chunks (MBMH, MBBB, MBNV, MBMI) are already defined in
`chunk_id.rs` as "MoP 5.0+" but are also used in WoD.

**Proposed patch:**

```rust
// In AdtVersion enum, add:
WoD,

// In detect_from_chunks, before the MoP check, add WoD-specific detection.
// However, there is no known ADT chunk that is unique to WoD and absent in
// MoP. Both use MTXP. The ADT MVER is 18 for ALL versions. WoD did not
// introduce a new ADT-level chunk that would distinguish it.
//
// Therefore, the practical approach is: accept that WoD ADTs are detected
// as MoP. This is correct behavior -- the binary format is the same.
// The WoD variant could be added to the enum for API completeness but
// would only be selectable via from_expansion_name("wod"), not auto-detected.

// In from_expansion_name, add:
"wod" | "draenor" | "warlords" | "warlords_of_draenor" => Some(Self::WoD),

// In as_str, add:
Self::WoD => "Warlords of Draenor 6.x",

// In version_range, add:
Self::WoD => "6.0.0 - 6.2.4",

// In expansion_name, add:
Self::WoD => "Warlords of Draenor",
```

**Verdict:** WoD ADTs already parse (as MoP). Adding the enum variant is
cosmetic/API-completeness but does not change parsing behavior.

---

### 6. DBC/DB2 Database Files -- PARTIAL, NEEDS WDB6

**Files:**
- `file-formats/database/wow-cdbc/src/versions.rs`
- `file-formats/database/wow-cdbc/src/parser.rs`

**Current behavior:**

```rust
pub enum DbcVersion {
    WDBC,  // Vanilla through WotLK
    WDB2,  // Cataclysm
    WDB3,  // (defined but routes through WDB2 parser path)
    WDB4,  // (defined but routes through WDB2 parser path)
    WDB5,  // MoP
}
```

The detect() method matches magic bytes: b"WDBC", b"WDB2", b"WDB3",
b"WDB4", b"WDB5". Anything else returns an error.

The parser dispatches:
- WDBC -> DbcHeader parser
- WDB2 -> Wdb2Header parser (also handles WDB3/WDB4 via conversion)
- WDB5 -> Wdb5Header parser

**WoD database format landscape:**

WoD (6.x) uses two DB2 format versions:
- **WDB5** -- used for many WoD database files. ALREADY SUPPORTED.
- **WDB6** -- introduced in WoD 6.0.2 for some tables. NOT SUPPORTED.

WDB6 differences from WDB5:
- Same 48-byte base header as WDB5
- Adds a "non-zero column" bitmask after the field structure array
- Adds column-level compression metadata (common data, pallet data)
- The record data may use dictionary/pallet compression per-column

**Proposed patch (partial WDB6 stub):**

```rust
// In DbcVersion enum, add:
WDB6,

// In detect(), add match arm:
b"WDB6" => Ok(DbcVersion::WDB6),

// In magic(), add:
DbcVersion::WDB6 => *b"WDB6",

// In parser.rs dispatch, add:
DbcVersion::WDB6 => {
    return Err(Error::InvalidHeader(
        "WDB6 format detected but not yet fully supported. \
         WDB6 adds column compression metadata beyond WDB5. \
         Use WDB5 files where available.".into()
    ));
}
```

This recognizes WDB6 files gracefully instead of failing with "Unknown DBC
version". Full WDB6 parsing requires implementing:
- Non-zero column bitmask parsing
- Pallet data decompression
- Common data column handling
- Copy table support

**Estimated effort:** Medium (200-400 lines of new parsing code, following
the WDB5 parser as a template). The WoWDev wiki documents the format fully.

**Verdict:** WDB5 WoD files already work. WDB6 needs new parser code.

---

### 7. MPQ Archives -- N/A FOR WoD

**File:** `file-formats/archives/wow-mpq/`

The MPQ crate supports format versions 1-4, covering all MPQ-era clients
(1.x through 5.x). MPQ v4 with HET/BET tables is fully implemented.

**WoD does not use MPQ.** Starting with WoD 6.0.2, Blizzard switched to
the CASC (Content Addressable Storage Container) archive format. There are
no MPQ files to read from a WoD installation.

The MPQ crate is correct and complete for its domain. No changes needed.

---

### 8. CASC Archives -- NOT IMPLEMENTED (FUNDAMENTAL BLOCKER)

There is no CASC crate in the workspace. This is the **single largest
barrier** to WoD support.

**What CASC requires:**
- Index file parsing (.idx files with content-addressable hashes)
- Data file reading (.data files with block-based storage)
- Encoding file parsing (maps content hash -> encoded file key)
- Root file parsing (maps filename hash -> content hash)
- BLTE container decompression (block table + zlib/lz4 segments)
- Download manifest parsing (for CDN-based access)
- Optional: CDN download support for streaming from Blizzard servers

**Estimated effort:** Large (2000-5000 lines, comparable to the MPQ crate).
Reference implementations exist:
- CascLib (C, by Ladislav Zezula -- same author as StormLib)
- CASC.NET (C#, by TOM_RUS)
- js-casc (JavaScript/TypeScript)

**Recommendation:** A `wow-casc` crate would be a natural addition to the
`file-formats/archives/` directory, parallel to `wow-mpq`. It could share
compression algorithm code (zlib is already in wow-mpq).

---

## Concrete Patch (Minimal)

The following changes have clear value and minimal risk:

### Change 1: M2 version comment fix
**File:** `file-formats/graphics/wow-m2/src/version.rs`

```diff
 // In from_header_version():
-272 => Some(Self::Cataclysm), // Cataclysm 4.3.4 and MoP 5.4.8
+272 => Some(Self::Cataclysm), // Cataclysm 4.3.4, MoP 5.4.8, and WoD 6.x

 // In to_header_version():
-Self::WoD => 275,          // Theoretical WoD chunked version
+Self::WoD => 272,          // WoD uses same M2 header version as Cata/MoP
```

### Change 2: ADT WoD variant (API completeness)
**File:** `file-formats/world-data/wow-adt/src/version.rs`

```diff
 pub enum AdtVersion {
     VanillaEarly,
     VanillaLate,
     TBC,
     WotLK,
     Cataclysm,
     MoP,
+    WoD,
 }
```

Plus corresponding match arms in `as_str`, `version_range`,
`from_expansion_name`, and `expansion_name`.

Detection note: WoD ADTs cannot be auto-distinguished from MoP ADTs by
chunk presence alone. `detect_from_chunks` should NOT be changed -- WoD
files correctly parse under the MoP path.

### Change 3: WDB6 recognition in DBC parser
**File:** `file-formats/database/wow-cdbc/src/versions.rs`

```diff
 pub enum DbcVersion {
     WDBC,
     WDB2,
     WDB3,
     WDB4,
     WDB5,
+    WDB6,
 }
```

```diff
 // In detect():
 b"WDB5" => Ok(DbcVersion::WDB5),
+b"WDB6" => Ok(DbcVersion::WDB6),
```

```diff
 // In magic():
 DbcVersion::WDB5 => *b"WDB5",
+DbcVersion::WDB6 => *b"WDB6",
```

**File:** `file-formats/database/wow-cdbc/src/parser.rs`

```diff
 DbcVersion::WDB5 => {
     // existing WDB5 handling
 }
+DbcVersion::WDB6 => {
+    return Err(Error::InvalidHeader(
+        "WDB6 format detected (Warlords of Draenor). \
+         WDB6 column compression is not yet implemented. \
+         Many WoD tables also ship as WDB5 -- try those files instead."
+            .into(),
+    ));
+}
```

### Change 4: Version support documentation update
**File:** `docs/src/resources/version-support.md`

Add a section clarifying WoD partial support:

> **Warlords of Draenor (6.x) -- Partial Support**
>
> Most WoD file formats (BLP, M2, WMO, WDT, ADT) use the same binary
> structures as MoP and parse correctly with existing code. Two gaps remain:
>
> 1. **WDB6 database files** are recognized but not parsed (WDB5 files work)
> 2. **CASC archives** are not implemented -- WoD game data cannot be
>    extracted from a WoD client installation without an external CASC tool

---

## Testing Strategy

1. **BLP:** Feed a WoD-era BLP2 file (e.g., extracted via CascViewer).
   Expect identical output to a MoP BLP2 file with same encoding.

2. **M2:** Feed a WoD-era .m2 file. Verify it parses with MD20 magic,
   version 272, and all fields read correctly. Compare against MoP .m2 of
   similar complexity.

3. **WMO:** Feed a WoD-era .wmo file. Verify MVER=18 is accepted and
   mapped to WmoVersion::Wod. Expect some unknown chunks to be skipped.

4. **DBC/DB2:** Feed a WoD WDB5 file -- expect it to parse. Feed a WoD
   WDB6 file -- expect a clear error message, not a crash.

5. **ADT:** Feed a WoD-era ADT file (pre-extracted). Verify it parses as
   MoP/WoD variant without errors. Unknown chunks should be skipped.

Test data: Extract WoD files using CascLib/CascViewer from a 6.2.4 client,
or use community-provided test fixtures.
