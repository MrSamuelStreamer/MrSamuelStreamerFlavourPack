# ISSUE 018: Uncached DefDatabase Lookups in Tick/Update Paths

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Performance |
| **Files** | Multiple |
| **Locations** | `MSSFP-VFE/HediffSwitchMapComponent.cs`, `MSSFP-VET/GameComponent_Tribals_Patch.cs`, `MSSFP-VET/RitualOutcomeEffectWorker_AdvanceToArcho.cs` |

## Description

Several files perform `DefDatabase<T>.GetNamed()` or `DefDatabase<T>.AllDefsListForReading.FirstOrDefault()` calls in tick methods or frequently-called code paths without caching the result.

Specific instances:
1. **HediffSwitchMapComponent**: `DefDatabase<HediffDef>.AllDefsListForReading.FirstOrDefault(...)` in a property getter called every 600 ticks. This performs a linear scan of all HediffDefs each time.
2. **GameComponent_Tribals_Patch**: Calls `DefDatabase<PreceptDef>.GetNamed("MSSFP_AdvanceToArcho")` twice (lines ~2146 and ~2153) in the same method.
3. **RitualOutcomeEffectWorker_AdvanceToArcho**: Uses `DefDatabase<PreceptDef>.GetNamed(...)` without caching.

## Impact

`DefDatabase<T>.GetNamed()` performs a dictionary lookup (fast but unnecessary to repeat). `AllDefsListForReading.FirstOrDefault()` performs a linear scan (slow). In tick paths, these add up.

## Suggested Fix

Cache all def lookups in static fields initialized once at startup:

```csharp
private static readonly HediffDef _cachedDef = 
    DefDatabase<HediffDef>.GetNamed("MyDefName");
```

Or use `[DefOf]` static classes for defs referenced by string name.


## Resolution

**Status**: 🔶 PARTIAL — 2026-04-06 (`misc_fixes`)

- `GameComponent_Tribals_Patch.cs`: duplicate `GetNamed("MSSFP_AdvanceToArcho")` calls replaced with a single local variable.
- `RitualOutcomeEffectWorker_AdvanceToArcho.cs`: `PreceptDef` lookup cached in a static field.
- `HediffSwitchMapComponent.cs`: `AllDefsListForReading.FirstOrDefault(...)` scan cached in a static field (addressed as part of issue 032 fix).

All originally identified locations are resolved.
