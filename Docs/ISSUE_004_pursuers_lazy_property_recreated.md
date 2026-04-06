# ISSUE 004: ScenPart_Pursuers Creates New Lazy on Every Property Access

| Field | Value |
|---|---|
| **Severity** | HIGH |
| **Category** | C# Code Quality |
| **File** | `1.6/Source/MSSFP/Questing/ScenPart_Pursuers.cs` |
| **Lines** | ~169 |

## Description

The `PursuersModExt` property is defined as:

```csharp
public Lazy<PursuersModExtension> PursuersModExt => new(...);
```

Using `=>` (expression-bodied property) means a **new `Lazy<T>` instance** is created on every access. The `Lazy<T>` wrapper never gets a chance to cache its value because it is discarded after each call. This defeats the entire purpose of using `Lazy<T>`.

## Impact

Every access to `PursuersModExt` re-executes the factory delegate (likely a `GetModExtension<T>()` call). If this property is accessed in tick paths or UI drawing, the repeated allocation and lookup adds unnecessary GC pressure and CPU cost.

## Suggested Fix

Change to a field with initializer, or use a backing field:

```csharp
private Lazy<PursuersModExtension> _pursuersModExt;

public Lazy<PursuersModExtension> PursuersModExt => 
    _pursuersModExt ??= new Lazy<PursuersModExtension>(...);
```


## Resolution

**Status**: ✅ FIXED — 2026-04-06 (`misc_fixes`)

Added a `_pursuersModExt` backing field with `??=` initialisation. The `Lazy<PursuersModExtension>` is now created once and cached; subsequent property accesses return the same instance.
