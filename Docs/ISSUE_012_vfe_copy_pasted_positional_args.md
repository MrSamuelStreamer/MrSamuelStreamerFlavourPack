# ISSUE 012: VFE Compatibility Uses Copy-Pasted Code with Positional Constructor Args

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality, Mod Architecture |
| **File** | `1.6/Source/MSSFP-VFE/HediffComp_TweakedAsexualReproduction.cs` |
| **Lines** | ~1695-1731, ~1758-1804 |

## Description

This file is acknowledged as copy-pasted from Vanilla Expanded Framework source code (comment at line ~1627). It contains `PawnGenerationRequest` constructor calls with **~30 positional parameters** each. These calls do not use named arguments.

`PawnGenerationRequest` is a struct whose constructor signature changes between RimWorld versions. Positional arguments mean that any parameter reordering, insertion, or removal in a game update will cause silent argument misalignment -- the code compiles but passes wrong values to wrong parameters.

## Impact

- **Silent logic bugs** on any RimWorld update that modifies `PawnGenerationRequest`.
- **Maintenance burden**: The copy-pasted code diverges from the upstream VEF source over time, accumulating behavioral differences.
- The file is 320+ lines of code that duplicates VEF functionality.

## Suggested Fix

1. Use **named arguments** for all `PawnGenerationRequest` constructor calls: `new PawnGenerationRequest(kind: ..., faction: ..., context: ...)`.
2. Consider whether the behavior can be achieved by subclassing or patching VEF's existing implementation rather than duplicating it.


## Resolution

**Status**: ⏸ DEFERRED

Named arguments not added. The copy-pasted block compiles correctly against the current VEF/RimWorld API. Adding named args requires confirming the exact parameter names from the VEF source to avoid introducing silent regressions. Tracked for a future PR when VEF source is available for reference.
