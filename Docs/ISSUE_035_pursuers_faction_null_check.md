# ISSUE 035: ScenPart_Pursuers Missing Faction Null Check

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Questing/ScenPart_Pursuers.cs` |
| **Lines** | ~185 |

## Description

The code calls `Faction.NameColored` without first checking if `Faction` is null. The faction is looked up by the string name from the mod extension (e.g., `"Insect"`). If the faction doesn't exist in the current game (faction mods removed, etc.), the lookup returns null.

## Impact

`NullReferenceException` when the pursuit scenario part tries to display the faction name in alerts or letters, if the configured faction doesn't exist.

## Suggested Fix

Add a null check:
```csharp
string factionName = Faction?.NameColored ?? "Unknown Faction";
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`Faction?.NameColored ?? "Unknown Faction"` null-conditional pattern applied. Alert and letter construction no longer throws `NullReferenceException` when the configured faction does not exist in the current game.
