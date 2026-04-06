# ISSUE 008: TransferableUIUtility Patch Fully Replaces DoRow via Prefix

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | Harmony Patches Quality |
| **File** | `1.6/Source/MSSFP/HarmonyPatches/TransferableUIUtility_PricePerMassColumn_Patch.cs` |
| **Lines** | ~164+ |

## Description

The active patch uses a `Prefix` that returns `false`, which completely replaces `TransferableOneWayWidget.DoRow`. This is the most fragile type of Harmony patch -- it takes full ownership of a complex UI rendering method.

Additionally, lines 24-161 contain a **large commented-out block** of what appears to be a previous implementation attempt.

## Impact

- **Any RimWorld update** that modifies `DoRow` will be silently overridden. New features, bug fixes, or UI changes to the transfer dialog will not take effect.
- Other mods that patch `DoRow` via Postfix will never execute, since the original method is skipped.
- The commented-out code adds 140 lines of dead weight.

## Suggested Fix

1. Use a Transpiler to inject only the specific modifications needed, rather than replacing the entire method.
2. If a full replacement is truly necessary, document the exact RimWorld version it was written against and add version checks.
3. Remove the commented-out code block.


## Resolution

**Status**: ⏸ DEFERRED

The full method replacement via Prefix is still in place. A transpiler rewrite requires in-game UI validation to confirm visual parity. The large commented-out code block also remains. Tracked for a future PR with in-game testing.
