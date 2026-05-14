# MSSFP Unity Asset Project

Unity project that builds custom shader AssetBundles for MSSFP. Shaders need a
real Unity compile — they can't ship as `.shader` source loaded from the C# mod.

## How it works

RimWorld 1.6 **auto-loads** AssetBundles from mod folders. Convention:

- **Bundle location**: `<mod>/Common/AssetBundles/` (or `<mod>/1.6/AssetBundles/`)
- **Bundle naming**: `<name>_linux`, `<name>_mac`, `<name>_win` per platform
- **Asset paths**: shaders live in `Assets/Data/MSSFP/Materials/` inside this
  project. At runtime, the C# code resolves them via
  `ContentFinder<Shader>.Get("ShaderName")`.

No manual `AssetBundle.LoadFromFile()` needed.

## Setup

1. Install **Unity 2022.3.35f1** — must match RimWorld 1.6's bundled Unity exactly.
   - Unity Hub → Installs → Archive → 2022.3.35f1
2. Open this folder (`UnityAssets/`) as a Unity project.

## Building

1. Menu: **Assets > Build MSSFP AssetBundles (LZ4)**
2. Bundles land in `../Common/AssetBundles/`:
   - `mssfp_linux`
   - `mssfp_mac`
   - `mssfp_win`

## Contents

- `Assets/Data/MSSFP/Materials/HoloMono.shader` — holo pawn shader:
  grayscale × tint, alpha pulse, optional outline + glow keywords.
- `Assets/Editor/ModAssetBundleBuilder.cs` — per-platform bundle builder.
- `Assets/Editor/AssetLabeler.cs` — labels everything in `Assets/Data/`.

## CI / batchmode

```
Unity -batchmode -projectPath ./UnityAssets \
      -executeMethod ModAssetBundleBuilder.BuildBundles \
      --assetBundleName=mssfp -quit
```
