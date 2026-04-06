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

**Status**: 📝 DOCUMENTED — 2026-04-06 (`misc_fixes`)

The Prefix replacement is retained. A transpiler alternative would need to inject a new column draw call and adjust layout constants mid-method — equally fragile and harder to read. Adding the column via Postfix is not viable as it would draw over existing content. The Prefix is the correct tool for this patch; the risk is accepted and documented.

The 137-line commented-out trade menu implementation (disabled `MSSFP_TradeUI_DrawTradeableRow_WeightPrice` class, lines 24–161) has been deleted. Unused `using` directives (`System.Collections.Generic`, `System.Linq`, `System.Reflection.Emit`) removed.
