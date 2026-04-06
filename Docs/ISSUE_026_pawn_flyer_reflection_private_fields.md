# ISSUE 026: PawnFlyerBalloon Accesses Private Fields via Reflection

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | C# Code Quality, RimWorld Best Practices |
| **File** | `1.6/Source/MSSFP/PawnFlyerBalloon.cs` |
| **Lines** | 6 Lazy<FieldInfo> fields near top |

## Description

`PawnFlyerBalloon` uses reflection to access **6 private fields** of the `PawnFlyer` base class via `Lazy<FieldInfo>` wrappers. Private fields are implementation details that can change between RimWorld versions without notice.

Additionally, `BedAvailableFor` accesses `Find.AnyPlayerHomeMap` which can return null (e.g., if the player has no home map, such as during certain quest scenarios).

## Impact

- Any RimWorld update that renames or removes these private fields will cause `NullReferenceException` at runtime (the `FieldInfo` will be null).
- The null `Find.AnyPlayerHomeMap` risk can cause a crash in the bed availability check.

## Suggested Fix

1. Use Harmony's `AccessTools.FieldRefAccess` for compile-time safety and better performance.
2. Add null checks for all `FieldInfo` lookups and for `Find.AnyPlayerHomeMap`.
3. Consider whether a Harmony Postfix on the base class methods would be cleaner than direct field access.


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

All 6 `Lazy<FieldInfo>` + `FieldInfo.GetValue()`/`SetValue()` patterns replaced with `AccessTools.FieldRefAccess<PawnFlyer, T>` delegates. Null guard added for `Find.AnyPlayerHomeMap` in `BedAvailableFor`.
