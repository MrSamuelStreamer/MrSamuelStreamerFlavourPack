# ISSUE 022: LINQ Allocations in Tick and Update Paths

| Field | Value |
|---|---|
| **Severity** | MEDIUM |
| **Category** | Performance |
| **Files** | Multiple |
| **Locations** | `MSSFPGameManager.cs:82-84`, `PersistentGeneGameComponent.cs`, `PossessionMapComponent.cs`, `Gene_GrassToucher.cs` |

## Description

Multiple tick-path methods use LINQ chains (`.Where()`, `.ToList()`, `.SelectMany()`, `.OfType()`) that allocate iterators, delegates, and temporary lists on every call.

Specific instances:
1. **MSSFPGameManager.GameComponentUpdate** (line 82-84): `.Where().ToList()` to find due tasks -- allocates a list every update frame.
2. **PersistentGeneGameComponent**: `.Where().ToList()` every 600 ticks.
3. **PossessionMapComponent**: `SelectMany` + `OfType` chain on tick.
4. **Gene_GrassToucher.Tick**: `RadialDistinctThingsAround` + `.OfType<Plant>()` every 60 ticks per pawn with the gene.

## Impact

Each LINQ chain allocates at least one iterator and one closure object per invocation. In tick paths, these short-lived allocations add GC pressure that compounds across multiple features and many pawns.

## Suggested Fix

Replace hot-path LINQ with explicit `for`/`foreach` loops and pre-allocated lists:

```csharp
// Instead of: var due = tasks.Where(t => t.IsDue).ToList();
_dueBuffer.Clear();
foreach (var task in tasks)
{
    if (task.IsDue) _dueBuffer.Add(task);
}
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Fixed:
- `PersistentGeneGameComponent.cs`: `.Where().ToList()` replaced with explicit `foreach` + pre-allocated buffer.
- `PossessionMapComponent.cs`: `SelectMany` + `OfType` chain replaced with explicit `foreach`.
- `Gene_GrassToucher.cs`: `RadialDistinctThingsAround + .OfType<Plant>()` in `Tick` replaced with explicit `foreach`.

Remaining: `MSSFPGameManager.GameComponentUpdate` lines 82–84 (`.Where().ToList()`) — now fixed. Replaced with a reverse-index `for` loop using `RemoveAt(i)`, eliminating all allocations. Unused `using System.Linq` removed from the file.
