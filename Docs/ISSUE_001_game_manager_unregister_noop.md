# ISSUE 001: MSSFPGameManager UnregisterAll Methods Are No-Ops

| Field | Value |
|---|---|
| **Severity** | CRITICAL |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/MSSFPGameManager.cs` |
| **Lines** | 187-201 |

## Description

`UnregisterAllOnThreadTask` and `UnregisterAllOffThreadTask` call `.ToList().RemoveAll(...)`. The `.ToList()` creates a **copy** of the dictionary's key-value pairs, then `RemoveAll` operates on that copy. The original dictionaries (`OnThreadTasks` and `OffThreadTasks`) are never modified. These methods silently do nothing.

## Code

```csharp
public void UnregisterAllOnThreadTask(string key)
{
    OnThreadTasks.ToList().RemoveAll(x => x.Key == key);
}

public void UnregisterAllOffThreadTask(string key)
{
    OffThreadTasks.ToList().RemoveAll(x => x.Key == key);
}
```

## Impact

Any code that calls these methods to clean up scheduled tasks is ineffective. Tasks that should be removed will continue to execute, potentially causing duplicate actions, stale references, or unexpected behavior after the task's context is no longer valid.

## Suggested Fix

Remove from the original dictionary directly:

```csharp
public void UnregisterAllOnThreadTask(string key)
{
    OnThreadTasks.Remove(key);
}
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

`UnregisterAll` methods now iterate `Manager.ScheduledOnThreadTasks.Values` / `Manager.ScheduledOffThreadTasks.Values` directly, calling `list.Remove(task)` on each entry. Tasks are now removed from the original collections rather than from discarded copies.
