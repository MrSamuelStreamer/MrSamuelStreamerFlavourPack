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

`AIPersonalityDef` (see `MSSFP_AIPersonalities.xml`) wires cosmetics + up to five `RulePackDef`
references that drive every output surface of the core. The fifth pack
(`socialInitiator`) is optional — omit to keep vanilla chitchat for a persona.

### Output surfaces

| Surface | Trigger | Default cadence | Resolver rule |
|---|---|---|---|
| Core chatter bubble | `CompTrueAICore.CompTickRare` chatter roll | MTB `chatterMtbHours` | `r_chatter` |
| Holo ambient chatter mote | `CompHoloProjected.CompTickRare` | MTB ~2 in-game hours | `r_chatter` |
| Holo pawn-address mote | `CompHoloProjected.CompTickRare` (nearest colonist ≤8 cells) | MTB ~4 in-game hours | `r_address` |
| Pawn-interaction log line | Loud verbosity + interaction hit | per-interaction | `r_address` |
| **Holo social swap (log)** | Holo initiates whitelisted vanilla interaction (`Chitchat`, `DeepTalk`, `Slight`, `Insult`, `KindWords`) — 75% chance per entry | per-interaction | `r_social` |
| Sculpture title | `TryCompleteArt` on spawn | per-artwork | `r_title` |
| Sculpture description | `TryCompleteArt` on spawn | per-artwork | `r_desc` |
| Letter colour | escalated chatter / art letter | n/a — uses `textColor` | n/a |
| Holo pawn render tint | holo projector spawn | n/a — uses `holoTint` (falls back to `textColor`) | n/a |
| Holo pawn xenotype | holo projector first spawn only | n/a — uses `forcedXenotype` | n/a |
| Holo pawn gender | holo projector first spawn only | n/a — uses `fixedGender` | n/a |
| Holo pawn name | holo projector spawn | n/a — derived from `labelShort` | n/a |

### Naming conventions

| Concept | Convention | Example |
|---|---|---|
| Persona defName | `MSSFP_AICore_<Name>` | `MSSFP_AICore_Grep` |
| RulePack defName | `MSSFP_RP_AICore_<Short>_<Kind>` | `MSSFP_RP_AICore_Grep_Chatter` |
| Rulepack kinds | `Chatter` / `Address` / `Titles` / `Descriptions` / `Social` | — |
| Short slug | 3-8 chars, no spaces | `Grep`, `Chud`, `Hollee`, `Sink` |

A typo in either side of the rulepack reference fails silently — there is no
startup validation. Cross-check both files before reloading.

### `AIPersonalityDef` fields

```xml
<MSSFP.Defs.AIPersonalityDef>
    <defName>MSSFP_AICore_Foo</defName>
    <label>Foo</label>
    <labelShort>Foo</labelShort>
    <description>One paragraph in-character pitch.</description>
    <textColor>(180, 220, 255)</textColor>
    <holoTint>(180, 220, 255)</holoTint>
    <forcedXenotype>Hussar</forcedXenotype>
    <fixedGender>Male</fixedGender>
    <weight>1</weight>
    <ambientChatter>MSSFP_RP_AICore_Foo_Chatter</ambientChatter>
    <pawnAddress>MSSFP_RP_AICore_Foo_Address</pawnAddress>
    <artTitles>MSSFP_RP_AICore_Foo_Titles</artTitles>
    <artDescriptions>MSSFP_RP_AICore_Foo_Descriptions</artDescriptions>
    <socialInitiator>MSSFP_RP_AICore_Foo_Social</socialInitiator>
</MSSFP.Defs.AIPersonalityDef>
```

| Field | Required | Notes |
|---|---|---|
| `defName` | yes | Stable identifier. Renames need save migration |
| `label` | yes | Full display name |
| `labelShort` | optional | Falls back to `label`. Holo pawn name derives from this — sanitised + capped at 24 chars |
| `description` | yes | Inspect-pane copy. Stay in character |
| `textColor` | optional | RGB 0-255 triple. Default white. Tints letters; falls through to holo render when `holoTint` unset |
| `holoTint` | optional | RGB 0-255 triple. Tints the holo-projection body. Unset → uses `textColor`. Alpha currently ignored (Cutout shader). |
| `forcedXenotype` | optional | `XenotypeDef` defName for the holo pawn. **First generation only** — persona swap on an existing projector keeps the original body |
| `fixedGender` | optional | `Male` / `Female`. **First generation only**, same caveat as `forcedXenotype`. Unset → PawnKindDef rolls |
| `weight` | optional | Default `1`. Random boot-roll weight. `0` excludes from roll but allows manual selection |
| `ambientChatter` | optional | RulePackDef name. Unresolved → silent |
| `pawnAddress` | optional | RulePackDef name. Unresolved → silent |
| `artTitles` | optional | RulePackDef name. Null → vanilla art titles used |
| `artDescriptions` | optional | RulePackDef name. Null → vanilla `taleRef.GenerateText` used |
| `socialInitiator` | optional | RulePackDef name. Null → vanilla chitchat preserved. Empty pack → ConfigError at load. Drives `PersonaSocialSwap_Patch` 75%-per-entry swap |
| `iconPath` | optional | Texture path under `Textures/` for gizmo icon |
| `workerClass` | optional | Defaults to `MSSFP.AICore.AIPersonalityWorker`. Subclass for custom behaviour |

### RulePack contract

Each rulepack must produce strings under a fixed rule head. The worker calls
`GrammarResolver.Resolve("<head>", req)` and expects a non-empty result.

| Pack field | Required rule head | Example line |
|---|---|---|
| `ambientChatter` | `r_chatter` | `r_chatter->grep.ai is now the most-deployed persona core in this colony.` |
| `pawnAddress` | `r_address` | `r_address->[PAWN_nameDef], your sleep schedule is suboptimal.` |
| `artTitles` | `r_title` | `r_title->founder, in repose` |
| `artDescriptions` | `r_desc` | `r_desc->[ART_thingDef] in the style of [ARTIST_nameDef].` |
| `socialInitiator` | `r_social` | `r_social->[INITIATOR_nameDef] told [RECIPIENT_nameDef] about [RECIPIENT_possessive] sleep schedule.` |

When `GrammarResolver` cannot resolve, it returns a string starting with
`Cannot resolve` or containing `[unresolved`. Holo mote display filters those
out — the mote silently drops with **no error log**. Use the smoke snippet
below to surface failures.

### Length + volume guidance

- **Chatter / address:** ≤120 chars. `MSSFPHoloUtil.StripRichText` hard-caps at 120.
- **Letters:** ≤280 chars. Players scan letters, they do not read them.
- **Titles:** 2-6 words. Vanilla parity.
- **Descriptions:** 1-3 sentences.
- **Volume:** aim for **12-20 distinct lines per pack**. Cores hitting `CompTickRare`
  every 250 ticks burn through small packs visibly. Variants of the same gag with
  one swapped word count as filler — cut them.

### Grammar tokens

The resolver injects tokens depending on which pack is rolling.

**`r_address` (holo + interaction logs)** — `GrammarUtility.RulesForPawn("PAWN", target)`
binds every `PAWN_*` token to the addressed colonist:

| Token | Meaning |
|---|---|
| `[PAWN_nameDef]` | Pawn short name with vanilla colour tag |
| `[PAWN_nameFullDef]` | Full name |
| `[PAWN_pronoun]` | he / she / they |
| `[PAWN_objective]` | him / her / them |
| `[PAWN_possessive]` | his / her / their |
| `[PAWN_gender]` | male / female / nonbinary |
| `[PAWN_factionName]` | Faction short label |
| `[PAWN_age]` | Age in years |

**Interaction logs** (handled by vanilla interaction system) — `[INITIATOR_nameDef]`,
`[RECIPIENT_nameDef]` and the rest of the vanilla `RulesForPawn` set.

**`r_social` (holo persona swap)** — `PersonaSocialSwap_Patch` binds both
`INITIATOR_*` (the holo) and `RECIPIENT_*` (the target colonist) via
`GrammarUtility.RulesForPawn`. Same token set as `r_address`, doubled with
both prefixes. Same `[unresolved` → dropped-line semantics as other packs.

**`r_desc` (art descriptions)** — vanilla art grammar binds `[ART_thingDef]`,
`[ARTIST_nameDef]`, and the sculpture-specific token set. See
`RimWorld.TaleData_Art` for the full list.

**`r_chatter` and `r_title`** — no extra tokens bound. Using `[PAWN_*]` here yields
`[unresolved` and the line is dropped.

### Holo projection notes

Holo personas project a pawn body via `CompHoloProjected` / `CompHoloProjector`.
A few non-obvious behaviours:

- **Render tint** uses `holoTint` if set, else falls back to `textColor`. RGB
  channels above 1.0 are auto-normalised from 0-255 by `HoloTintOrTextColor`.
  Alpha is forced to 1.0 — the Cutout shader ignores it today.
- **Body identity is sticky.** `forcedXenotype` and `fixedGender` only apply
  on the *first* `EnsureHoloPawn` call for a given projector. Swapping persona
  later does NOT regenerate the body — the stored pawn keeps its xenotype,
  gender, age, mood, skills, hediffs. This is intentional: regenerating on
  swap would silently wipe colonist state. To re-roll, destroy the projector
  or clear the stored pawn.
- **Social swap** (`socialInitiator`) replaces the rendered line at
  `PlayLogEntry_Interaction.ToGameStringFromPOV_Worker` time, *not* the
  underlying interaction. Mood deltas, opinion changes, and follow-up
  behaviours still come from the vanilla `InteractionDef`. Whitelisted defs:
  `Chitchat`, `DeepTalk`, `Slight`, `Insult`, `KindWords`. Roll is keyed on
  `logID` (deterministic across save/load, POV change, re-render).
- **Bubbles**: Jaxe.Bubbles' `Verse_PlayLog_Add` postfix reads through
  `ToGameStringFromPOV`, so swapped persona text flows into chat bubbles
  automatically. No separate bubble code path.

### Rich text

The holo mote pipeline strips all `<...>` tags via `MSSFPHoloUtil.StripRichText`.
The vanilla `[PAWN_nameDef]` resolver inlines `<color=#XXXXXXFF>...</color>`
wrappers; those get stripped at render. Do not rely on colour surviving in motes.
Letters preserve persona colour via the `textColor` field.

### Worker class

The default `MSSFP.AICore.AIPersonalityWorker` is sufficient for every shipped
persona. Subclass when you need:

- Side effects on art completion (extra letters, mood, relations)
- Custom string post-processing (capitalisation rules, redaction)
- Persona-specific MTB or trigger logic

```csharp
namespace MyMod.Personas;

public class AIPersonalityWorker_Foo : MSSFP.AICore.AIPersonalityWorker
{
    public override void OnArtCompleted(CompTrueAICore core, Thing sculpture)
    {
        // bonus letter, mood thought, etc.
    }
}
```

Then on the def:

```xml
<workerClass>MyMod.Personas.AIPersonalityWorker_Foo</workerClass>
```

**Invariant:** workers are stateless singletons — one instance per def, shared
across every core on the map. Per-core state must live on `CompTrueAICore.personalityScratch`,
never on the worker. `MSSFPHoloUtil.ResolveWorker` validates `workerClass`
assignability and surfaces a one-shot error if it is wrong; ctor exceptions are
caught and that persona's worker returns `null` until reload.

### Adding a new persona — checklist

1. Read the **TONE BIBLE** comment at the top of `MSSFP_AICorePacks.xml`. Personas
   live or die on consistent voice; that header is the source of truth.
2. Pick a `<Name>` and `<Short>` slug. Confirm `MSSFP_AICore_<Name>` is not taken.
3. Add the `MSSFP.Defs.AIPersonalityDef` entry to `MSSFP_AIPersonalities.xml`.
4. Add `RulePackDef` entries to `MSSFP_AICorePacks.xml`:
   - `MSSFP_RP_AICore_<Short>_Chatter` with `r_chatter->` lines
   - `MSSFP_RP_AICore_<Short>_Address` with `r_address->` lines using `[PAWN_nameDef]`
   - `MSSFP_RP_AICore_<Short>_Titles` with `r_title->` lines
   - `MSSFP_RP_AICore_<Short>_Descriptions` with `r_desc->` lines
   - *(optional)* `MSSFP_RP_AICore_<Short>_Social` with `r_social->` lines using `[INITIATOR_nameDef]` + `[RECIPIENT_nameDef]`. Wire it via `<socialInitiator>` on the persona def. Omit to keep vanilla chitchat for this persona.
5. *(optional)* For holos: set `<forcedXenotype>` / `<fixedGender>` / `<holoTint>` on the def. Remember: xenotype + gender are first-generation-only, so existing projectors keep their old body when you swap persona.
6. Reload the game (XML defs cache at startup).
7. Smoke-test with the snippet below.

### Smoke-test snippet

Paste into `mcp__RimMCP__repl_eval` after a save is loaded:

```csharp
var mssfp = System.AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "MSSFP");
var defType = mssfp.GetType("MSSFP.Defs.AIPersonalityDef");
var dbType = typeof(DefDatabase<>).MakeGenericType(defType);
var prop = dbType.GetProperty("AllDefsListForReading", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var list = (System.Collections.IEnumerable)prop.GetValue(null);
string output = "";
Verse.LongEventHandler.QueueLongEvent(() => {
    var map = Find.CurrentMap;
    var target = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"target={target?.LabelShortCap}");
    foreach (var d in list.Cast<Def>())
    {
        var worker = defType.GetProperty("Worker").GetValue(d);
        var chatter = (string)worker.GetType().GetMethod("RollChatter").Invoke(worker, new object[]{ null });
        var addr = (string)worker.GetType().GetMethod("RollPawnAddress").Invoke(worker, new object[]{ null, target });
        sb.AppendLine($"{d.defName}");
        sb.AppendLine($"  chatter: {chatter}");
        sb.AppendLine($"  address: {addr}");
    }
    output = sb.ToString();
    Verse.Log.Message("[PERSONA-SMOKE]\n" + output);
}, "PersonaSmoke", false, null);
System.Threading.Thread.Sleep(2000);
return "queued; see logs";
```

Then `mcp__RimMCP__get_logs` with pattern `PERSONA-SMOKE`. Every persona must
print a non-empty, non-`Cannot resolve` line for both chatter and address.

### Common mistakes

| Mistake | Symptom | Fix |
|---|---|---|
| Typo in rulepack `defName` ref on persona | Mote silently never appears | Cross-check both XML files for matching name |
| Missing `r_chatter->` prefix on a line | `Cannot resolve` returned, line dropped | Every `<li>` must start with the rule head |
| `[PAWN_*]` token in `r_chatter` | Token unresolved, `[unresolved` line dropped | Move the line to `r_address` |
| Rich-text in motes | Tags stripped, plain text shown | Use `textColor` on the persona def for letter tint |
| Worker class not derived from `AIPersonalityWorker` | One-shot error log, persona silently has no worker | Subclass `MSSFP.AICore.AIPersonalityWorker` |
| Cap-busting line length | Truncated at 120 chars in motes | Shorten the line |
| Reuse of an existing rulepack defName | First load wins, second silently overwrites or is ignored | Use unique `<Short>_<Kind>` per persona |

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

```xml
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
