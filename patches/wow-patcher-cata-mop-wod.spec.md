# wow-patcher Extension Specification: Original Cata / MoP / WoD Client Support

> Specification for extending [wowemulation-dev/wow-patcher](https://github.com/wowemulation-dev/wow-patcher)
> to support the original Cataclysm 4.3.4 (build 15595), Mists of Pandaria 5.4.8
> (build 18414), and Warlords of Draenor 6.2.4 (build 21742) client executables.
>
> Date: 2026-08-17

---

## 1. Current Architecture Overview

wow-patcher is a Rust-based binary patcher that modifies WoW executables for
private server connectivity. It supports two patching modes:

### 1.1 Static Binary Patching (default)

Reads a PE/Mach-O executable, locates byte patterns in safe data sections
(`.rdata`, `.data`, `__DATA`), replaces them, and writes the patched binary
to disk. Only data sections are modified; `.text` is excluded because runtime
protections (Arxan) would reject the changes.

### 1.2 Runtime Patching (Windows-only, `launch` subcommand)

Launches the client in a suspended state, waits for Arxan decryption of the
`.text` section, then applies in-memory patches via `WriteProcessMemory`
before resuming execution. This handles integrity checks, certificate
validation bypasses, and encrypted code sections.

### 1.3 Pattern System

Patterns are `Vec<i16>` where literal bytes are 0-255 and `-1` is a wildcard.
The patcher searches the binary for these anchor patterns, then overwrites
the surrounding region with replacement data. Current patterns:

| Pattern                   | Anchor Bytes (hex)                        | Replacement Size | Purpose                                    |
|---------------------------|-------------------------------------------|------------------|--------------------------------------------|
| ConnectTo RSA Modulus     | `91 D5 9B B7 D4 E1 83 A5`                | 256 bytes        | Replace Blizzard's RSA public modulus       |
| Signature RSA Modulus     | `35 FF 17 E7 33 C4 D3 D4`                | 256 bytes        | Replace signature verification modulus      |
| Crypto RSA Modulus        | `71 FD FA 60 14 0D F2 05`                | 256 bytes        | Replace crypto/warden RSA modulus           |
| Ed25519 Public Key        | `15 D6 18 BD 7D B5 77 BD`                | 32 bytes         | Replace Ed25519 signing key                 |
| Portal Domain             | `.actual.battle.net` (string)             | 19 bytes + NUL   | Redirect BGS Aurora-RPC portal              |
| Version URL (v1)          | `http://%s.patch.battle.net:1119/...`     | Variable         | Redirect TACT version endpoint              |
| Version URL (v2)          | `https://%s.version.battle.net/v2/...`    | Variable         | Redirect TACT version endpoint              |
| Version URL (v3)          | Unified API format                        | Variable         | Redirect unified TACT endpoint              |
| CDNs URL                  | `http://%s.patch.battle.net:1119/%s/cdns` | Variable         | Redirect TACT CDN list endpoint             |
| Cert Bundle               | `{"Created":` (JSON envelope)             | Up to 32761 bytes| Replace embedded certificate bundle         |
| Cert Bundle URL           | `http://nydus.battle.net/Bnet/zxx/...`    | 59 bytes         | Redirect cert download URL                  |

### 1.4 Client Detection

Client type is detected by path heuristics (`_retail_`, `_classic_`,
`_classic_era_`). Version is extracted from the PE resource
`VS_VERSIONINFO` (StringFileInfo FileVersion or ProductVersion, then
VsFixedFileInfo fallback).

### 1.5 Currently Supported Clients

All supported clients are **Classic re-releases** using the modern Battle.net
authentication infrastructure:

| Version Range  | Era               | Auth System  | File System |
|----------------|-------------------|-------------|-------------|
| 1.13.x-1.14.x | Vanilla Classic   | Battle.net  | CASC        |
| 2.5.x-2.5.4   | TBC Classic       | Battle.net  | CASC        |
| 3.4.x-3.4.4   | WotLK Classic     | Battle.net  | CASC        |
| 4.4.x-4.4.2   | Cataclysm Classic | Battle.net  | CASC        |

---

## 2. Target Client Analysis

The three original expansion clients have fundamentally different
authentication and file system architectures from the Classic re-releases.

### 2.1 Cataclysm 4.3.4 (Build 15595) -- December 2012

**Authentication:** SRP6 (Secure Remote Password protocol)
- The client connects to an "auth server" (formerly "logon server")
  specified by the `realmlist` setting
- No Battle.net portal integration
- No OAuth, no Battle.net gateway
- TrinityCore provides `authserver` for this protocol

**File System:** MPQ (Mo'PaQ archives)
- No CASC, no TACT versioning endpoints
- No version/CDN URL patterns exist in the binary

**Protection:** None (no Arxan, no code section encryption)
- The `.text` section is plain unencrypted code
- No integrity checks to bypass
- Static binary patching of `.text` is viable

**Crypto:** Server-side SRP6 + session key derivation
- No Battle.net RSA modulus (the `91 D5 9B B7...` pattern will NOT exist)
- No Ed25519 key
- No certificate bundle mechanism
- Authentication uses SRP6 verifier exchange, not RSA key pinning

**What needs patching:**
1. **Hardcoded realmlist string** -- The binary contains a default realmlist
   hostname (e.g., `us.logon.battle.net` or `us.actual.battle.net`) that
   must be replaced with the private server address. This is a simple
   string replacement in `.rdata`/`.data`.
2. **Realmlist WTF override** -- Alternatively, many Cata servers simply
   instruct users to edit `WTF/realmlist.wtf`. However, some builds
   ignore the WTF file and use the hardcoded value, so binary patching
   is the reliable approach.
3. **Signature/checksum verification** (optional) -- Some builds verify
   the integrity of core DLLs or the executable itself on launch. This
   would require NOP'ing out a check in `.text`, which is safe since
   there is no Arxan protection.

### 2.2 Mists of Pandaria 5.4.8 (Build 18414) -- September 2013

**Authentication:** SRP6 (same as Cataclysm)
- Still uses the classic auth server protocol
- Battle.net integration was NOT yet live for authentication
- The client connects via `realmlist` to an auth server
- TrinityCore provides `authserver` for MoP

**File System:** MPQ archives
- No CASC
- No TACT version/CDN endpoints
- No version URL patterns in the binary

**Protection:** Minimal
- No Arxan code encryption in the original retail 5.4.8
- The `.text` section is standard unencrypted PE code
- Static `.text` patching is viable

**Crypto:** Same SRP6 model as Cataclysm
- No Battle.net RSA modulus
- No Ed25519 key
- No certificate bundle

**What needs patching:**
1. **Hardcoded realmlist/portal string** -- Similar to Cata, the binary
   contains a default server hostname. Replace with private server address.
2. **Realmlist WTF** -- Can be edited manually, but binary patch is more
   reliable.
3. **Warden module loading** (optional) -- The client attempts to load and
   run the Warden anti-cheat module from Blizzard servers. For private
   servers that do not implement Warden, this can cause connection drops.
   Patching the Warden initialization call to NOP is sometimes needed.

### 2.3 Warlords of Draenor 6.2.4 (Build 21742) -- June 2016

**Authentication:** Battle.net OAuth2
- WoD was the first expansion to use Battle.net authentication exclusively
- The client connects to a Battle.net portal for OAuth
- TrinityCore provides `bnetserver` alongside `worldserver` for WoD

**File System:** CASC (Content Addressable Storage Container)
- Uses TACT (Tooling for Accessing Client Trees) version/CDN endpoints
- Version URL patterns exist in the binary
- CDN URL patterns exist in the binary

**Protection:** Arxan (partial -- early deployment)
- WoD introduced Arxan anti-tamper protection
- The `.text` section may be encrypted at rest
- Runtime patching may be needed for `.text` modifications
- Integrity checks present but less sophisticated than later versions

**Crypto:** Battle.net RSA + early Bnet crypto
- **RSA modulus IS present** -- the ConnectTo RSA modulus pattern
  (`91 D5 9B B7 D4 E1 83 A5`) should exist in this binary since it uses
  the same Battle.net infrastructure
- **Ed25519 MAY or may not be present** -- Ed25519 was introduced later;
  WoD 6.2.4 may predate its deployment
- Certificate bundle may be present in a different format than Classic
  re-releases

**What needs patching:**
1. **RSA Modulus** -- Same ConnectTo modulus replacement as current clients
   (VERIFY: the 8-byte prefix must be confirmed in the actual binary)
2. **Portal Domain** -- `.actual.battle.net` string replacement
   (VERIFY: confirm the exact portal string format in WoD)
3. **Version URL** -- Likely the v1 format
   (`http://%s.patch.battle.net:1119/%s/versions`)
4. **CDNs URL** -- Standard TACT CDN endpoint redirect
5. **Integrity checks** -- Runtime NOP of Arxan integrity verification
   (requires the `launch` subcommand approach)
6. **Signature modulus** (VERIFY: may use a different modulus than the
   one currently targeted)

---

## 3. Implementation Plan

### 3.1 New ClientType Variants

Extend `src/platform/mod.rs`:

```rust
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ClientType {
    Retail,
    Classic,
    ClassicEra,
    // New variants for original expansion clients
    OriginalCata,    // 4.3.4 build 15595
    OriginalMoP,     // 5.4.8 build 18414
    OriginalWoD,     // 6.2.4 build 21742
    Unknown,
}
```

Client type detection must move beyond path heuristics to version-based
detection. When `extract_version()` returns a version, match on it:

```rust
fn classify_by_version(v: &Version) -> ClientType {
    match (v.major, v.minor, v.patch, v.build) {
        // Original Cataclysm
        (4, 3, 4, 15595) => ClientType::OriginalCata,
        // Original MoP
        (5, 4, 8, 18414) => ClientType::OriginalMoP,
        // Original WoD
        (6, 2, 4, 21742) => ClientType::OriginalWoD,
        // Cataclysm Classic (re-release)
        (4, 4, _, _) => ClientType::Classic,
        // MoP Classic (re-release)
        (5, 5, _, _) => ClientType::Classic,
        // ... existing logic
        _ => ClientType::Unknown,
    }
}
```

### 3.2 New PatchGroup: REALMLIST

For pre-Battle.net clients (Cata 4.3.4, MoP 5.4.8), add a new patch group:

```rust
bitflags! {
    pub struct PatchGroup: u32 {
        const RSA            = 1 << 0;
        const ED25519        = 1 << 1;
        const PORTAL         = 1 << 2;
        const VERSION        = 1 << 3;
        const CDNS           = 1 << 4;
        const CERT_BUNDLE    = 1 << 5;
        const CERT_BUNDLE_URL = 1 << 6;
        // New
        const REALMLIST      = 1 << 7;  // Hardcoded realmlist string replacement
        const WARDEN_DISABLE = 1 << 8;  // Optional: NOP Warden init for 4.x/5.x
    }
}
```

### 3.3 New Pattern Definitions

Add to `src/patterns/mod.rs` (or a new `src/patterns/legacy.rs` module):

```rust
// --- Cata 4.3.4 patterns (MUST BE VERIFIED against actual binary) ---

/// Hardcoded realmlist hostname in Cata 4.3.4.
/// Candidates (verify by hexdump of Wow.exe):
///   "us.logon.battle.net"   (US client)
///   "eu.logon.battle.net"   (EU client)
///   "kr.logon.battle.net"   (KR client)
///   "tw.logon.battle.net"   (TW client)
/// The pattern should match the common suffix ".logon.battle.net"
/// to work across all regional clients.
pub fn cata_realmlist_pattern() -> Pattern {
    pattern_from_str(".logon.battle.net")
}

// --- MoP 5.4.8 patterns (MUST BE VERIFIED against actual binary) ---

/// Hardcoded realmlist hostname in MoP 5.4.8.
/// Same format as Cata: "<region>.logon.battle.net"
pub fn mop_realmlist_pattern() -> Pattern {
    pattern_from_str(".logon.battle.net")
}

// --- WoD 6.2.4 patterns (MUST BE VERIFIED against actual binary) ---

/// WoD 6.2.4 uses Battle.net authentication.
/// The portal pattern may be identical to existing clients:
///   ".actual.battle.net"
/// Or may use an earlier format. VERIFY against the actual binary.
///
/// The RSA modulus pattern (91 D5 9B B7...) should also be present.
/// VERIFY: the exact same 8-byte prefix may or may not match.
```

### 3.4 Version-Dependent Patch Selection

The `execute_patch()` function in `src/cmd/execute.rs` must select which
patch groups are applicable based on the detected client version:

```rust
fn applicable_patches(client_type: ClientType) -> PatchGroup {
    match client_type {
        ClientType::OriginalCata | ClientType::OriginalMoP => {
            // Pre-Battle.net clients: only realmlist patching
            PatchGroup::REALMLIST
            // Optionally: | PatchGroup::WARDEN_DISABLE
        }
        ClientType::OriginalWoD => {
            // WoD uses Battle.net but may have different patterns
            PatchGroup::RSA | PatchGroup::PORTAL
                | PatchGroup::VERSION | PatchGroup::CDNS
            // No ED25519 (predates its deployment)
            // No CERT_BUNDLE (different mechanism)
        }
        ClientType::Classic | ClientType::ClassicEra => {
            // Current behavior: all Battle.net patches
            PatchGroup::all()
        }
        ClientType::Retail | ClientType::Unknown => {
            PatchGroup::all()
        }
    }
}
```

### 3.5 Realmlist Patching Implementation

New patching logic for pre-Battle.net clients:

```rust
/// Patch the hardcoded realmlist hostname in a pre-Battle.net client.
///
/// The original string (e.g., "us.logon.battle.net") is replaced with
/// a private server hostname, NUL-padded to match the original length.
///
/// The replacement hostname must be <= the original length. If the
/// private server hostname is shorter, it is NUL-padded. If longer,
/// the patch is rejected.
fn patch_realmlist(
    data: &mut [u8],
    original_pattern: &[i16],
    replacement_host: &str,
) -> Result<usize, WowPatcherError> {
    // Find the pattern
    let offset = data.find_pattern(original_pattern)
        .ok_or_else(|| WowPatcherError::new(
            ErrorCategory::PatchingError,
            "Realmlist pattern not found in binary"
        ))?;

    // The full original string extends before the pattern match
    // (e.g., "us" prefix before ".logon.battle.net")
    // We need to find the start of the full hostname
    // ... (implementation depends on exact binary layout)

    Ok(1)
}
```

### 3.6 CLI Extensions

Add a `--realmlist` flag for pre-Battle.net clients:

```rust
/// Private server hostname for pre-Battle.net clients (Cata 4.3.4, MoP 5.4.8).
/// Replaces the hardcoded realmlist hostname in the binary.
/// Example: --realmlist "127.0.0.1" or --realmlist "myserver.example.com"
#[arg(long, env = "WOW_REALMLIST")]
realmlist: Option<String>,
```

---

## 4. Data Collection Requirements

Before any of this can be implemented, the following data must be gathered
from actual client binaries. This is the critical blocking dependency.

### 4.1 Cata 4.3.4 (Build 15595) -- Wow.exe

Run a hex editor or `strings` analysis on the original Wow.exe:

1. **Realmlist string** -- Search for `logon.battle.net` or `actual.battle.net`
   - Record: exact string, file offset, containing PE section
   - Note: there may be multiple regional variants (US/EU/KR/TW)
   - Check both `.rdata` and `.data` sections

2. **Battle.net RSA modulus** -- Search for `91 D5 9B B7 D4 E1 83 A5`
   - If found: record offset, this client can use existing RSA patching
   - If NOT found: confirm this client predates Battle.net RSA

3. **SRP6 parameters** -- The SRP6 generator `g` and prime `N` are
   hardcoded. TrinityCore already knows these values, so patching them
   is not needed (the server uses the same parameters).

4. **Checksum/integrity verification** -- Launch the client and observe
   if it self-verifies before connecting. If yes, locate the check
   routine in `.text` and document the pattern for NOP'ing.

5. **Warden initialization** -- Search for Warden-related strings
   (e.g., `Warden`, `Module`, `ScanResult`). If present, document the
   init call site for optional disabling.

**Tool suggestion:** Use `wow-patcher dump-sections` on the original binary,
then analyze with a disassembler (IDA Free, Ghidra, x64dbg).

### 4.2 MoP 5.4.8 (Build 18414) -- Wow.exe / Wow-64.exe

Same analysis as Cata, plus:

1. **64-bit variant** -- MoP 5.4.8 ships both 32-bit (`Wow.exe`) and
   64-bit (`Wow-64.exe`). Both need separate pattern analysis since
   offsets differ and instruction encoding varies.

2. **Portal strings** -- Check for `.actual.battle.net` or
   `bnet.battle.net`. MoP 5.4.8 was late enough that early Battle.net
   integration code may exist in the binary even if unused at runtime.

3. **CASC markers** -- Search for `data/data` or `.idx` file references.
   If absent, confirms MPQ-only file system (expected for 5.4.8).

### 4.3 WoD 6.2.4 (Build 21742) -- Wow.exe / Wow-64.exe

Full analysis needed since this client uses Battle.net:

1. **RSA modulus** -- Search for `91 D5 9B B7 D4 E1 83 A5`
   - If found: existing pattern works, record offset and section
   - If different prefix: document the new 8-byte prefix

2. **Ed25519 key** -- Search for `15 D6 18 BD 7D B5 77 BD`
   - If found: record offset (unexpected for WoD era)
   - If NOT found: confirm Ed25519 is absent (expected)

3. **Portal domain** -- Search for `.actual.battle.net`
   - Record exact string, offset, section
   - Note any differences from the Classic re-release format

4. **Version URL** -- Search for `patch.battle.net` and `version.battle.net`
   - Determine which URL format (v1/v2/v3) is used
   - Record the exact URL template string

5. **CDNs URL** -- Search for `patch.battle.net:1119` + `cdns`
   - Record exact string and offset

6. **Cert bundle** -- Search for `{"Created":`
   - If present: this client uses the same cert mechanism
   - If absent: document the alternative cert approach

7. **Arxan protection** -- Launch the client under a debugger
   - Observe if `.text` is encrypted at rest
   - If encrypted: the `launch` runtime approach is required
   - Document the Arxan variant version if determinable

8. **Integrity patterns** -- If Arxan is present, identify the
   runtime integrity check patterns. These may differ from the
   patterns in `src/patterns/runtime/windows.rs`.

---

## 5. Architectural Decisions

### 5.1 Two-Track Architecture

The extension naturally splits into two tracks:

**Track A: Pre-Battle.net Clients (Cata 4.3.4, MoP 5.4.8)**
- Simple realmlist string replacement
- No crypto key patching needed
- No TACT/CDN URL patching needed
- Static binary patching only (no Arxan)
- Minimal integration with the existing patcher

**Track B: Battle.net Client (WoD 6.2.4)**
- Extension of existing Battle.net patching
- RSA modulus replacement (possibly same pattern)
- Portal domain replacement
- TACT URL replacement
- Arxan runtime patching may be needed
- Deeper integration with existing architecture

### 5.2 Pattern Configuration vs. Hardcoded Patterns

The current architecture hardcodes all patterns as Rust constants. For
multi-version support, consider moving to a configuration-driven approach:

```rust
/// Version-specific pattern set loaded at runtime
struct VersionPatterns {
    /// Build number this pattern set applies to
    build: u32,
    /// Authentication model
    auth_type: AuthType,  // SRP6 or BattleNet
    /// Patterns to search for (may be empty if not applicable)
    realmlist: Option<Pattern>,
    connect_to_modulus: Option<Pattern>,
    signature_modulus: Option<Pattern>,
    ed25519_key: Option<Pattern>,
    portal_domain: Option<Pattern>,
    version_url: Option<Pattern>,
    cdns_url: Option<Pattern>,
    cert_bundle: Option<Pattern>,
}

enum AuthType {
    /// Classic SRP6 auth server (Cata 4.3.4, MoP 5.4.8)
    SRP6,
    /// Battle.net OAuth (WoD 6.2.4+)
    BattleNet,
}
```

This would allow new versions to be supported by adding pattern data
without modifying the core patching logic.

### 5.3 Section Restrictions

The current section validation (only `.rdata`/`.data` are patchable)
works for the Classic re-releases because all target data resides in
data sections. For original clients:

- **Cata/MoP:** The realmlist string is typically in `.rdata` (safe).
  Any integrity check NOP'ing would require `.text` access, which the
  static patcher currently forbids. Either:
  (a) Add a `--allow-text-patches` flag, or
  (b) Use the `launch` runtime patcher even for non-Arxan clients

- **WoD:** Same as current -- data in `.rdata`/`.data`, code patches
  via runtime mode.

---

## 6. Testing Strategy

### 6.1 Binary Fixtures

Create test fixtures for each version:
- Craft synthetic PE files with known patterns at known offsets
- Use the existing test infrastructure (`tests/integration_test.rs`)
- Add version-specific test cases

### 6.2 Integration Tests

For each target version, the integration test matrix should cover:
- Pattern detection (does `find_pattern` locate the target?)
- Patch application (does `patch_with_padding` write correctly?)
- Section validation (is the target in a patchable section?)
- Version detection (does `extract_version` parse the PE correctly?)
- Client type classification (does `classify_by_version` return the
  right variant?)

### 6.3 End-to-End Validation

Requires actual client binaries (cannot be distributed):
- Patch each version
- Verify the patched binary launches
- Verify it connects to the private server auth/bnet endpoint
- Verify gameplay session establishment

---

## 7. Risk Assessment

### 7.1 High Confidence (WoD 6.2.4)

WoD uses the same Battle.net infrastructure as the currently supported
clients. The patterns are likely identical or very similar. The main risk
is Arxan protection differences requiring updated runtime patterns.

**Estimated effort:** 1-2 weeks once binary patterns are confirmed.

### 7.2 Medium Confidence (MoP 5.4.8)

MoP uses a simpler auth model (SRP6) that requires a different patching
approach (realmlist string replacement). The implementation is
straightforward, but:
- The exact realmlist string format must be confirmed
- Both 32-bit and 64-bit binaries need analysis
- Warden handling may be needed

**Estimated effort:** 1 week once binary patterns are confirmed.

### 7.3 Medium Confidence (Cata 4.3.4)

Same SRP6 model as MoP. Simpler (32-bit only for the common build).
The main risk is that some Cata builds have multiple hardcoded realmlist
entries or regional variants that all need patching.

**Estimated effort:** 3-5 days once binary patterns are confirmed.

### 7.4 Blocking Dependency

All three versions are blocked on **binary pattern extraction from actual
client executables**. This requires:
1. Legal copies of each client version
2. Hex analysis / reverse engineering of each binary
3. Documentation of exact byte patterns, offsets, and sections

---

## 8. File Change Summary

| File                         | Change Type | Description                                |
|------------------------------|-------------|--------------------------------------------|
| `src/platform/mod.rs`        | Modify      | Add OriginalCata/MoP/WoD client types      |
| `src/patch_group.rs`         | Modify      | Add REALMLIST, WARDEN_DISABLE flags         |
| `src/patterns/mod.rs`        | Modify      | Add legacy pattern statics                  |
| `src/patterns/legacy.rs`     | **New**     | Pre-Battle.net pattern definitions          |
| `src/cli.rs`                 | Modify      | Add --realmlist flag                        |
| `src/patcher.rs`             | Modify      | Add realmlist builder method                |
| `src/cmd/execute.rs`         | Modify      | Version-dependent patch selection           |
| `src/realmlist.rs`           | **New**     | Realmlist validation and config (like portal_domain.rs) |
| `tests/integration_test.rs`  | Modify      | Add version-specific test cases             |
| `docs/src/patches.md`        | Modify      | Document new patch types                    |

---

## 9. Appendix: Authentication Protocol Reference

### SRP6 Flow (Cata 4.3.4, MoP 5.4.8)

```
Client                          Auth Server
  |                                 |
  |--- AUTH_LOGON_CHALLENGE ------->|  (username, client build)
  |<-- AUTH_LOGON_CHALLENGE_REPLY --|  (B, g, N, s, security_flags)
  |                                 |
  |--- AUTH_LOGON_PROOF ----------->|  (A, M1, crc_hash)
  |<-- AUTH_LOGON_PROOF_REPLY ------|  (M2, account_flags)
  |                                 |
  |--- AUTH_REALMLIST_REQUEST ----->|
  |<-- AUTH_REALMLIST_RESPONSE -----|  (realm list)
  |                                 |
  |--- Connect to selected realm ->|
```

The `realmlist` hostname (hardcoded or from WTF) is the auth server
address used for the initial `AUTH_LOGON_CHALLENGE` TCP connection.
Patching this string is the ONLY modification needed for basic
connectivity.

### Battle.net OAuth Flow (WoD 6.2.4)

```
Client                          Battle.net Portal
  |                                 |
  |--- BGS ConnectRequest -------->|  (via Aurora-RPC)
  |<-- BGS ConnectResponse --------|  (session key, modulus)
  |                                 |
  |--- OAuth handshake ----------->|  (RSA-encrypted challenge)
  |<-- OAuth token ----------------|
  |                                 |
  |--- Realm selection ----------->|  (via TACT version/CDN)
  |<-- Realm list + CDN config ----|
  |                                 |
  |--- Connect to game server ---->|
```

The RSA modulus, portal domain, and TACT URLs are all involved in this
flow. Patching them is what the current patcher already does for Classic
re-releases; WoD 6.2.4 should be the closest match.
