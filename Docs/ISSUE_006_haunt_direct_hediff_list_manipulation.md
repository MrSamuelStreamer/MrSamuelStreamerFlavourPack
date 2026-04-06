# ISSUE 006: HediffComp_Haunt Directly Manipulates Hediff List

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality, RimWorld Best Practices |
| **File** | `1.6/Source/MSSFP/Hediffs/HediffComp_Haunt.cs` |
| **Lines** | ~93-118 |

## Description

`CompPostTick` directly manipulates `health.hediffSet.hediffs` (the internal hediff list) to transfer haunts between pawns. This bypasses RimWorld's hediff add/remove system, which handles:

- Dirty flag updates for health caching
- Notification callbacks (e.g., `PostAdd`, `PostRemoved`)
- UI updates and letter generation
- Save state consistency

## Impact

- **Save corruption**: The hediff set's internal state (caches, dirty flags) becomes inconsistent with the actual hediff list.
- **Null references**: Other systems that enumerate hediffs may encounter unexpected state.
- **Silent failures**: Hediff comps that rely on `PostAdd`/`PostRemoved` callbacks will not fire.

## Suggested Fix

Use the proper API:
```csharp
targetPawn.health.RemoveHediff(hediff);
newPawn.health.AddHediff(hediffDef, bodyPart);
```

If the hediff instance must be preserved (same severity, same comps), clone the relevant state into a new hediff instance rather than moving the object between lists.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Direct `hediffSet.hediffs` list manipulation replaced with `pawn.health.RemoveHediff()` and `pawn.health.AddHediff()`. Relevant hediff state (severity, comp fields) is transferred to the new hediff instance before the old one is removed. `PostAdd`/`PostRemoved` callbacks now fire correctly.

**Behaviour note**: Callbacks that were previously silent now execute. If haunt-specific `PostAdd`/`PostRemoved` logic triggers new notifications or letters, that is the correct behaviour.
