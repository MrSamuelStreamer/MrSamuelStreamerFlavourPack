# ISSUE 031: ReformationPointsWorldComponent Missing Null Guards

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Comps/World/ReformationPointsWorldComponent.cs` |

## Description

Accesses `Find.FactionManager.OfPlayer.ideos.PrimaryIdeo` without null guards. Several points in this chain can be null:
- `Find.FactionManager` during early initialization.
- `.OfPlayer` if the player faction hasn't been set up.
- `.ideos` if Ideology DLC is not active.
- `.PrimaryIdeo` if no primary ideology is set.

## Impact

`NullReferenceException` if the Ideology DLC is not active, or during game initialization before the player faction is fully set up.

## Suggested Fix

Add null-conditional operators and early returns:

```csharp
var primaryIdeo = Find.FactionManager?.OfPlayer?.ideos?.PrimaryIdeo;
if (primaryIdeo == null) return;
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Null-conditional guards added throughout the `Find.FactionManager?.OfPlayer?.ideos?.PrimaryIdeo` chain. All affected methods now return early when any link in the chain is null (e.g., when the Ideology DLC is not active or during early initialisation).
