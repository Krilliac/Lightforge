# wow-patcher Extension: Original Cata / MoP / WoD Support

## Overview

This specification describes what is needed to extend
[wowemulation-dev/wow-patcher](https://github.com/wowemulation-dev/wow-patcher)
to support the **original** Cataclysm 4.3.4 (build 15595), Mists of Pandaria
5.4.8 (build 18414), and Warlords of Draenor 6.2.4 (build 21742) client
executables.

A functional code patch is **not feasible** at this time because the binary
patterns for each client version must be extracted from the actual executables
through reverse engineering. The specification document contains the full
architectural analysis, implementation plan, and data collection checklist.

## Key Finding: Two Different Authentication Eras

The original expansion clients span two fundamentally different authentication
architectures:

### Pre-Battle.net (Cata 4.3.4, MoP 5.4.8)

These clients use the **SRP6 authentication protocol** and connect to an
auth server via a `realmlist` hostname. They do NOT use:

- Battle.net portal connections
- RSA key pinning (the `91 D5 9B B7...` pattern does not exist)
- Ed25519 signing keys
- TACT version/CDN endpoints
- CASC file system
- Certificate bundles
- Arxan anti-tamper protection

**Patching these clients requires a different approach:** replacing a
hardcoded realmlist hostname string in the binary. This is a simpler
operation than the Battle.net patching the tool currently performs, but
it requires new patch group types and CLI options.

### Battle.net Era (WoD 6.2.4)

WoD 6.2.4 uses **Battle.net OAuth authentication** -- the same
infrastructure the currently supported Classic re-releases use. The existing
patcher patterns (RSA modulus, portal domain, TACT URLs) are likely
applicable, though the exact byte patterns need verification.

WoD also introduced Arxan anti-tamper protection, meaning runtime patching
via the `launch` subcommand may be required for code-section modifications.

## Files

| File | Description |
|------|-------------|
| `wow-patcher-cata-mop-wod.spec.md` | Full specification with architecture analysis, implementation plan, pattern definitions, data collection requirements, and testing strategy |
| `wow-patcher-cata-mop-wod.README.md` | This file |

## What Needs to Happen Next

### Step 1: Binary Pattern Extraction (Blocking)

Obtain the original client executables and extract byte patterns:

**For Cata 4.3.4 and MoP 5.4.8:**
```
strings Wow.exe | grep -i "battle.net\|logon\|realmlist"
```
Document the exact realmlist hostname string, its file offset, and which PE
section it resides in.

**For WoD 6.2.4:**
```
# Check for existing patterns
python -c "
import sys
data = open('Wow-64.exe', 'rb').read()
patterns = {
    'ConnectTo RSA': bytes([0x91, 0xD5, 0x9B, 0xB7, 0xD4, 0xE1, 0x83, 0xA5]),
    'Ed25519':       bytes([0x15, 0xD6, 0x18, 0xBD, 0x7D, 0xB5, 0x77, 0xBD]),
    'Portal':        b'.actual.battle.net',
    'Version URL':   b'patch.battle.net',
    'CDNs URL':      b'patch.battle.net:1119',
    'Cert Bundle':   b'{\"Created\":',
}
for name, pat in patterns.items():
    idx = data.find(pat)
    print(f'{name}: {\"offset 0x\" + hex(idx)[2:] if idx >= 0 else \"NOT FOUND\"}')"
```

### Step 2: Implementation

Once patterns are confirmed, the implementation follows the plan in the spec:

1. Add new `ClientType` variants (`OriginalCata`, `OriginalMoP`, `OriginalWoD`)
2. Add `REALMLIST` patch group for pre-Battle.net clients
3. Add version-based client classification
4. Implement realmlist string patching
5. Verify WoD works with existing Battle.net patterns (or add WoD-specific ones)
6. Add CLI `--realmlist` flag
7. Write tests with synthetic PE fixtures

### Step 3: Testing

- Unit tests with synthetic binaries
- Integration tests against actual client executables (not distributable)
- End-to-end tests against TrinityCore authserver/bnetserver

## Estimated Effort

| Version | Complexity | Effort (after pattern extraction) |
|---------|-----------|-----------------------------------|
| Cata 4.3.4 | Low (realmlist string swap) | 3-5 days |
| MoP 5.4.8 | Low (realmlist string swap, 32+64 bit) | 1 week |
| WoD 6.2.4 | Medium (Battle.net + Arxan) | 1-2 weeks |

## Architecture Reference

See Section 1 of the spec for a complete breakdown of:
- The pattern matching system (`Vec<i16>` with wildcards)
- Current patch groups (RSA, ED25519, PORTAL, VERSION, CDNS, CERT_BUNDLE)
- Static vs. runtime patching modes
- Section validation rules (.rdata/.data only for static patches)
- TrinityCore default key material
