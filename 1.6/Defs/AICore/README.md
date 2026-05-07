# AI Core

How to attach the True AI Core feature to a building, and how the personality + sculpture pipeline hangs together.

---

## Attaching the AI Core to a building

Two comps are involved:

1. **`MSSFP.Comps.CompProperties_TrueAICore`** — goes on the host building (the "core" itself). Drives chatter, personality switching, and queues / completes the Create-AI-Art job.
2. **`MSSFP.Comps.CompProperties_TrueAICoreArt`** — goes on the *output* sculpture def. Holds the personality-flavoured description string. A Harmony postfix on `CompArt.GenerateImageDescription` substitutes it at read time.

### Minimum patch — host building

```xml
<li Class="PatchOperationAdd">
    <xpath>Defs/ThingDef[defName="YourBuildingDefName"]/comps</xpath>
    <value>
        <li Class="MSSFP.Comps.CompProperties_TrueAICore">
            <defaultPersonality>MSSFP_AICore_Grep</defaultPersonality>
            <artInputs>
                <li>WoodLog</li>
                <li>Steel</li>
            </artInputs>
            <artInputCount>30</artInputCount>
            <artOutputDef>MSSFP_AICoreSculptureSmall</artOutputDef>
        </li>
    </value>
</li>
```

The host building must also have `tickerType` of `Rare` or higher — `CompTrueAICore` uses `CompTickRare`. If the parent / abstract base sets `Never`, patch it:

```xml
<Operation Class="PatchOperationReplace">
    <xpath>Defs/ThingDef[defName="YourBuildingDefName"]/tickerType</xpath>
    <value><tickerType>Rare</tickerType></value>
</Operation>
```

### `CompProperties_TrueAICore` fields

| Field | Default | Purpose |
|-------|---------|---------|
| `defaultPersonality` | _null_ | `AIPersonalityDef` the core boots with. Null → comp picks one weighted at spawn. |
| `showPersonalitySelector` | `true` | Whether players see the "Switch personality" gizmo. |
| `chatterMtbHours` | `4.0` | MTB between ambient chatter rolls (Normal verbosity). |
| `verbosity` | `Normal` | `Quiet` / `Normal` / `Loud`. Loud halves MTB, Quiet doubles. |
| `chatterCooldownTicks` | `600` | Min ticks between consecutive chatter hits. Prevents bursts. |
| `letterChance` | `0.02` | Per-chatter probability of escalating to a Letter (suppressed during danger). |
| `lettersPerDay` | `1` | Hard cap on letters/day from this core. |
| `artInputs` | _null_ | List of `ThingDef`s accepted by the haul job. |
| `artInputCount` | `75` | Total stuff units consumed per sculpture. |
| `artOutputDef` | _null_ | The sculpture `ThingDef` to spawn. **Must carry `CompTrueAICoreArt`** to receive flavoured descriptions — otherwise vanilla `taleRef` text is used. |

> **Art is opt-in.** If `artInputs` is null/empty *or* `artOutputDef` is null, the "Create AI art" gizmo and haul job stay hidden. Use this for chatter-only cores.

---

## Personalities

`AIPersonalityDef` (see `MSSFP_AIPersonalities.xml`) wires four `RulePackDef` references:

| Field | Drives |
|-------|--------|
| `ambientChatter` | Lines emitted on chatter ticks. Bubbles above the core. |
| `pawnAddress` | Lines spoken at colonists when Loud verbosity hits an `InteractionDef`. |
| `artTitles` | Sculpture title generation in `TryCompleteArt`. |
| `artDescriptions` | Sculpture description string written to the sidecar comp. |

Add a personality:

1. Author four `RulePackDef`s in `MSSFP_AICorePacks.xml`. Use `<rulesStrings>` with grammar tokens — see existing packs for token names (e.g. `[colonist]`, `[founderTerm]`).
2. Add an `MSSFP.Defs.AIPersonalityDef` entry with the four refs + cosmetics (`label`, `labelShort`, `textColor`, `weight`).
3. Reference the new defName from a building's `<defaultPersonality>` if you want it as that core's boot persona. Otherwise the spawn roll picks weighted across all loaded personalities.

`weight` is a relative selection weight for the random boot roll. `0` excludes from the roll but still allows manual selection via the gizmo.

---

## Sculpture output

The default sculpture is `MSSFP_AICoreSculptureSmall` (this folder). It:

- Inherits `SculptureBase` → keeps vanilla `CompArt`, `CompQuality`, `CompStyleable`, `CompMeditationFocus`.
- Declares `<comps><li Class="MSSFP.Comps.CompProperties_TrueAICoreArt"/></comps>` inline. RimWorld merges `<comps>` lists across the inheritance chain — the sidecar appends without dropping inherited components.
- Reuses vanilla `Things/Building/Art/SculptureSmall` graphic until bespoke art ships.

### Adding a new sculpture output

```xml
<ThingDef ParentName="SculptureBase">
    <defName>YourAISculptureGrand</defName>
    <label>large AI-generated sculpture</label>
    <description>...</description>
    <graphicData>
        <graphicClass>Graphic_Random</graphicClass>
        <texPath>Things/Building/Art/SculptureLarge</texPath>
    </graphicData>
    <statBases>
        <MaxHitPoints>180</MaxHitPoints>
        <Beauty>120</Beauty>
        <WorkToMake>30000</WorkToMake>
    </statBases>
    <costStuffCount>100</costStuffCount>
    <comps>
        <li Class="MSSFP.Comps.CompProperties_TrueAICoreArt" />
    </comps>
</ThingDef>
```

Then point the host building at it via `<artOutputDef>YourAISculptureGrand</artOutputDef>`.

> **Do NOT patch `CompTrueAICoreArt` onto vanilla sculpture defs.** The sidecar attaches by inheritance only. Patching `SculptureBase` would shotgun every sculpture in the game. The current design keeps the comp scoped to AI-core outputs.

---

## Runtime pipeline

```
chatter tick ─┬─> RulePackDef.ambientChatter ─> AICoreSpeech.EmitMessage ─> bubble
              └─> letterChance roll ─> RulePackDef + LetterDef ─> Letter

Create AI art toggle (FloatMenu)
   ↓
artRequested = true
   ↓
WorkGiver_HaulToAICore + JobDef ─> colonists haul artInputs to innerContainer
   ↓
TryCompleteArt (CompTickRare) when RemainingNeed <= 0
   ↓ ThingMaker.MakeThing(artOutputDef, PickStuff())
   ↓ CompQuality.SetQuality(Awful)
   ↓ CompArt.InitializeArt + Title from artTitles RulePack
   ↓ CompTrueAICoreArt.flavouredDescription = artDescriptions RulePack output
   ↓ GenSpawn at adjacent cell (4-tier fallback)
   ↓ unused stuff drops back to world
```

The sidecar's `flavouredDescription` is `Scribe_Values.Look`'d, so it survives save/reload.

`CompArt.GenerateImageDescription` is intercepted by `Patch_CompArt_FlavouredDescription` (Harmony postfix). When the sidecar string is non-empty the postfix overrides `__result`. When null/empty the vanilla `taleRef.GenerateText` path runs — no log spam.

---

## Quick-test recipe

```

<Patch>
    <Operation Class="PatchOperationSequence">
        <operations>

            <!-- DiningChair inherits tickerType from ArtableFurnitureBase (default Never).
                 CompTickRare needs Rare or higher. Conditional handles has/missing. -->
            <li Class="PatchOperationConditional">
                <xpath>Defs/ThingDef[defName="DiningChair"]/tickerType</xpath>
                <match Class="PatchOperationReplace">
                    <xpath>Defs/ThingDef[defName="DiningChair"]/tickerType</xpath>
                    <value>
                        <tickerType>Rare</tickerType>
                    </value>
                </match>
                <nomatch Class="PatchOperationAdd">
                    <xpath>Defs/ThingDef[defName="DiningChair"]</xpath>
                    <value>
                        <tickerType>Rare</tickerType>
                    </value>
                </nomatch>
            </li>

            <!-- DiningChair already has <comps> (CompProperties_Styleable). Append our comp. -->
            <li Class="PatchOperationAdd">
                <xpath>Defs/ThingDef[defName="DiningChair"]/comps</xpath>
                <value>
                    <li Class="MSSFP.Comps.CompProperties_TrueAICore">
                        <defaultPersonality>MSSFP_AICore_Grep</defaultPersonality>
                        <showPersonalitySelector>true</showPersonalitySelector>
                        <chatterMtbHours>4.0</chatterMtbHours>
                        <verbosity>Normal</verbosity>
                        <chatterCooldownTicks>5000</chatterCooldownTicks>
                        <letterChance>0.02</letterChance>
                        <lettersPerDay>1</lettersPerDay>
                        <artInputs>
                            <li>WoodLog</li>
                            <li>Steel</li>
                        </artInputs>
                        <artInputCount>30</artInputCount>
                        <artOutputDef>MSSFP_AICoreSculptureSmall</artOutputDef>
                    </li>
                </value>
            </li>

        </operations>
    </Operation>
</Patch>
```
