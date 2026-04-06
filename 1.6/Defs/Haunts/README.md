# Haunts & Echos

This folder contains all defs for the two spirit systems in MSSFP: **Haunts** and **Echos**.
They share rendering and caching infrastructure but are conceptually and mechanically distinct.

---

## The Two Systems

### Haunts — Named and Dynamic Grave Spirits

A **named haunt** is the lingering spirit of a specific named character (Grignr, Frog, Kekvit,
Oskar, Phil, Jade). Named haunts are authored in XML with fixed stat effects per stage and a
specific awakening gene.

A **dynamic haunt** (`MSS_FP_PawnDisplayer`) is generated at runtime from *any* buried colonist.
The ghost renders as the actual dead colonist. Its archetype, progression trigger, stat effects,
and awakening gene are all derived at assignment time from that colonist's top skills — a melee
specialist produces a different ghost than a researcher. See the *Dynamic Colonist Haunts* section
below for full details.

Both types attach to living pawns who spend time near the relevant grave, grow in power over time
through aligned actions, and can reach an Awakened state where they imprint a permanent gene.

Haunts are **beneficial** (`isBad: false`). They carry stat effects that scale with severity, and
a ghost overlay is rendered near the host pawn when drafted.

### Echos — Body-Hop Spirits

An echo is a free-roaming spirit (`MSS_FP_Echo`) that hops between living hosts using the
Body Hop ability. Each time it moves, it leaves an imprint of the previous host's mind —
a `PawnInfo` entry recording their name, best skill, traits, and the date of the hop.

The current host receives cumulative skill boosts from every previous host in the echo's history.
Passed traits are applied directly. The **Spirit** inspector tab shows the full history.

Echos must be enabled in mod settings (`Enable the echo event`). They are **harmful**
(`isBad: true`) — the stat costs (rest drain, work speed penalty) represent the strain of
co-existing with a foreign presence.

---

## File Map

| File | Contents |
|------|----------|
| `MMSFP_HauntsGood.xml` | HediffDefs for all six named haunts + `MSS_FP_PawnDisplayer` (dynamic haunt carrier) |
| `MMSFP_HauntsBad.xml` | HediffDef for the echo (`MSS_FP_Echo`) + possessed thought |
| `MMSFP_HauntProgression.xml` | HauntProgressionDefs — progression rates and triggers per named haunt |
| `MMSFP_HauntSkillMappings.xml` | HauntSkillMappingDefs — skill → archetype/trigger/stats/gene for dynamic haunts |
| `MMSFP_HauntGenes.xml` | Susceptibility genes (6A), thought def (6B), named awakening genes (6C), generic awakening gene pool (6D) |
| `MMSFP_HauntArchetypes.xml` | The three archetypes: Aggressive, Trickster, Brooding |
| `MMSFP_HauntInteractions.xml` | Archetype pair interactions + iconic pair overrides |
| `MMSFP_HauntInteractionThoughts.xml` | ThoughtDefs used by interactions |
| `MMSFP_HauntInteractionHediffs.xml` | Temporary hediffs used by interactions (e.g. melee buff) |
| `MMSFP_HauntEvents.xml` | HauntEventDefs — the poltergeist event table |
| `MMSFP_HauntEventThoughts.xml` | ThoughtDefs used by poltergeist events |
| `MMSFP_ExorcismDefs.xml` | Thoughts and hediffs for exorcism outcomes |
| `MMSFP_ExorcismRecipes.xml` | RecipeDef for the exorcism medical operation |
| `MMSFP_HauntResearch.xml` | ResearchProjectDef for Spiritual Medicine |

---

## Haunt System — How It Works

### Assignment

`HauntedMapComponent` runs a periodic check (configurable interval, default every 2 days)
on the player's home map. It builds a pool of eligible pawns — humanlike colonists on the
map who are **not** HauntResistant — and picks one weighted by gene susceptibility
(HauntSensitive pawns are 1.5× more likely to be chosen).

The haunt is assigned by adding the corresponding `HediffDef` to the chosen pawn.

### Severity Stages

Every named haunt has three severity stages defined directly on the `HediffDef`:

| Stage | Severity range | Label pattern | Effect |
|-------|---------------|--------------|--------|
| Whisper | 0.00 – 0.33 | "whispered to by {Name}" | ~⅓ of full effect |
| Presence | 0.34 – 0.66 | "accompanied by {Name}" | ~⅔ of full effect |
| Awakened | 0.67 – 1.00 | "empowered by {Name}" | Full + bonus effects |

Stat effects are defined per stage using standard RimWorld `<statFactors>` / `<statOffsets>` /
`<painFactor>` on each `<li>` in `<stages>`.

### Progression

Severity increases passively over time and spikes when the host performs actions aligned
with the haunt's character. This is driven by `HediffCompProperties_HauntProgression`
(added as a second comp on every named haunt) and its corresponding `HauntProgressionDef`
in `MMSFP_HauntProgression.xml`.

**Passive rate:** `passiveSeverityPerDay` — small constant gain each day.

**Triggers:** Each progression def has a list of `<triggers>`. The only current trigger type
is `RecordDelta`, which fires when a pawn's record counter (kills, recruitments, etc.) increases
since the last check (every `triggerCheckIntervalTicks`, default 2500 ticks ≈ 1 hour).

```xml
<triggers>
    <li>
        <triggerType>RecordDelta</triggerType>
        <recordDef>KillsHumanlikes</recordDef>
        <progressionAmount>0.10</progressionAmount>
    </li>
</triggers>
```

**Regression:** If no trigger fires for `regressionThresholdDays`, severity decays by
`regressionPerDay` each day. Set `regressionThresholdDays` high (e.g. 10 for Phil) for
haunts that should be sticky, low (e.g. 3 for Jade) for haunts that fade quickly without
active play.

**Speed multipliers:** Both progression and regression are scaled by the mod settings sliders
(`HauntProgressionSpeedMultiplier`, `HauntRegressionSpeedMultiplier`). HauntSensitive gene
applies an additional 2× to progression; HauntResistant applies 0.5×.

### Awakening Gene

When a haunt reaches severity ≥ 0.67 for the first time, `HediffComp_HauntProgression`
fires `TryFireAwakeningGene()`. This:

1. Adds the `awakeningGeneDef` (defined in the progression def) as an endogene on the host
2. Shows a letter: "{Pawn} has been marked by the awakened essence of {Name}."
3. Sets `awakeningGeneFired = true` — will not fire again even if severity later drops and rises

The awakening genes are defined in `MMSFP_HauntGenes.xml` with permanent stat bonuses that
reflect the haunt's character.

### Ghost Rendering

The ghost overlay is controlled by `HediffCompProperties_Haunt` fields:

| Field | Purpose |
|-------|---------|
| `graphicData` | Texture path, draw size, shader (`MoteGlow` recommended) |
| `offsets` | Four `(x, y, z)` vectors for North/East/South/West facing |
| `onlyRenderWhenDrafted` | If true, ghost only appears when the pawn is drafted |
| `ambientFleck` | Optional: a FleckDef spawned periodically near the ghost |

Textures live in `Textures/Haunts/{Name}/` and must be multi-directional
(`Graphic_Multi` — four files: `_north`, `_east`, `_south`, `_west`).

### Archetype Interactions

Each haunt is assigned an archetype via the `<archetype>` field on
`HediffCompProperties_Haunt`. When two haunted pawns have a social interaction,
`HauntSocialInteraction_Patch` checks for matching interaction defs:

1. **Iconic pair overrides** are checked first (e.g. Frog + Grignr = Eternal Rivalry)
2. **Archetype pair interactions** are used as fallback (e.g. Aggressive + Trickster = Provocation)

The three archetypes are: `MSS_FP_Archetype_Aggressive`, `MSS_FP_Archetype_Trickster`,
`MSS_FP_Archetype_Brooding`.

### Poltergeist Events

`HauntEventMapComponent` ticks every 6 hours. It sums haunt severity across all haunted
pawns on the map (`mapIntensityScore`), applies a time-of-day multiplier (1.5× at night),
and fires a random event from the weighted table in `MMSFP_HauntEvents.xml` if the
effective intensity exceeds the threshold (default 0.5, configurable).

Event severity requirements act as gatekeeping — dramatic events (fire, possession,
manifestation) only fire at high severity.

### Exorcism

Research: `MSS_FP_SpiritualMedicine` (800 pts, requires Psychite Refining).

Operation: consumes 1× Psychite Tea + 10× Gold. The outcome is determined by the
doctor's Medicine skill vs. haunt severity:

- **Success:** haunt removed cleanly
- **Partial:** haunt removed, host suffers Spiritual Trauma thought
- **Failure:** haunt severity increases, minor mental break
- **Backlash:** on any removal, high-severity haunts have a chance to briefly terrify
  nearby pawns before departing

---

## Adding a New Named Haunt

To add a new haunt (e.g. "Mira the Scholar"), you need entries in four files:

### 1. `MMSFP_HauntsGood.xml` — The HediffDef

```xml
<HediffDef>
    <defName>MSS_FP_MiraHaunt</defName>
    <label>Guided by Mira</label>
    <description>The spirit of Mira lingers nearby, whispering knowledge.</description>
    <hediffClass>HediffWithComps</hediffClass>
    <defaultLabelColor>(0.1,0.2,0.8)</defaultLabelColor>
    <maxSeverity>1.0</maxSeverity>
    <scenarioCanAdd>true</scenarioCanAdd>
    <isBad>false</isBad>
    <stages>
        <!-- Whisper: 0.0–0.33 -->
        <li>
            <minSeverity>0</minSeverity>
            <label>whispered to by Mira</label>
            <statFactors>
                <ResearchSpeed>1.05</ResearchSpeed>
            </statFactors>
        </li>
        <!-- Presence: 0.34–0.66 -->
        <li>
            <minSeverity>0.34</minSeverity>
            <label>accompanied by Mira</label>
            <statFactors>
                <ResearchSpeed>1.10</ResearchSpeed>
            </statFactors>
        </li>
        <!-- Awakened: 0.67–1.0 -->
        <li>
            <minSeverity>0.67</minSeverity>
            <label>empowered by Mira</label>
            <statFactors>
                <ResearchSpeed>1.20</ResearchSpeed>
                <StudyEfficiency>1.15</StudyEfficiency>
            </statFactors>
        </li>
    </stages>
    <comps>
        <li Class="MSSFP.Hediffs.HediffCompProperties_Haunt">
            <graphicData>
                <texPath>Haunts/Mira/Mira</texPath>
                <graphicClass>Graphic_Multi</graphicClass>
                <drawSize>1.7</drawSize>
                <shaderType>MoteGlow</shaderType>
            </graphicData>
            <offsets>
                <li>(0.3, 1.6, -0.55)</li>  <!-- North -->
                <li>(-0.5, 1.1, -0.35)</li> <!-- East -->
                <li>(0.3, -1.6, 0.65)</li>  <!-- South -->
                <li>(0.5, 1.1, -0.35)</li>  <!-- West -->
            </offsets>
            <onlyRenderWhenDrafted>true</onlyRenderWhenDrafted>
            <archetype>MSS_FP_Archetype_Brooding</archetype>
        </li>
        <li Class="MSSFP.Hediffs.HediffCompProperties_HauntProgression" />
    </comps>
</HediffDef>
```

Note: `HediffCompProperties_HauntProgression` has no fields — it just registers the comp.
The actual rates come from the matching `HauntProgressionDef` below.

### 2. `MMSFP_HauntProgression.xml` — Progression Rates & Triggers

```xml
<MSSFP.Defs.HauntProgressionDef>
    <defName>MSS_FP_MiraHauntProgression</defName>
    <hauntDef>MSS_FP_MiraHaunt</hauntDef>
    <passiveSeverityPerDay>0.03</passiveSeverityPerDay>
    <regressionPerDay>0.015</regressionPerDay>
    <regressionThresholdDays>7</regressionThresholdDays>
    <triggerCheckIntervalTicks>2500</triggerCheckIntervalTicks>
    <awakeningGeneDef>MSS_FP_MiraAwakening</awakeningGeneDef>
    <triggers>
        <li>
            <triggerType>RecordDelta</triggerType>
            <recordDef>ResearchPointsDone</recordDef>  <!-- or another RecordDef -->
            <progressionAmount>0.10</progressionAmount>
        </li>
    </triggers>
</MSSFP.Defs.HauntProgressionDef>
```

Common `recordDef` values: `KillsHumanlikes`, `KillsAnimals`, `DamageDealt`, `DamageTaken`,
`PrisonersRecruited`, `AnimalsTamed`, `ThingsCrafted`, `OperationsPerformed`,
`ThingsConstructed`, `CellsMined`, `MealsCooked`, `PlantsSown`.
Note: `SocialFights` and `DamageDone` do **not** exist — use `DamageDealt` for ranged/melee damage dealt.

### 3. `MMSFP_HauntGenes.xml` — Awakening Gene

```xml
<GeneDef>
    <defName>MSS_FP_MiraAwakening</defName>
    <label>scholar's imprint</label>
    <description>Mira's scholarly spirit has left its mark. The carrier absorbs new knowledge with unusual speed.</description>
    <biostatMet>0</biostatMet>
    <statFactors>
        <ResearchSpeed>1.15</ResearchSpeed>
    </statFactors>
    <displayCategory>Haunts</displayCategory>
    <displayOrderInCategory>16</displayOrderInCategory>
</GeneDef>
```

Keep `biostatMet>0</biostatMet>` — awakening genes are permanent gifts, not metabolic costs.
Increment `displayOrderInCategory` beyond the last existing entry (currently 15 for Jade).

### 4. `MSSFPDefOf.cs` — DefOf Registration (C#)

Add a line in the `[DefOf]` class so the progression system can reference the haunt hediff:

```csharp
public static readonly HediffDef MSS_FP_MiraHaunt;
```

This is only strictly required if C# code needs to reference the def directly (e.g. for the
`HauntedMapComponent` assignment pool). The progression and interaction systems look up defs
by the string references in XML, so the XML-only parts work without it.

### 5. Textures

Add four directional sprites to `Textures/Haunts/Mira/`:
- `Mira_north.png`
- `Mira_east.png`
- `Mira_south.png`
- `Mira_west.png`

Recommended: semi-transparent, glowing, roughly humanoid silhouette at 256×256.
Use existing haunt textures as a reference for style and alpha levels.

---

## Tweaking Existing Haunts

### Change how fast a haunt grows
Edit `passiveSeverityPerDay` in `MMSFP_HauntProgression.xml`. Multiply the mod settings
`HauntProgressionSpeedMultiplier` slider on top of this for live tuning.

### Change how fast a haunt fades
Edit `regressionPerDay` and `regressionThresholdDays`. Setting `regressionThresholdDays`
to a high number (20+) effectively makes haunts permanent once acquired.

### Change trigger conditions or burst sizes
Edit `<triggers>` in `MMSFP_HauntProgression.xml`. Each trigger fires at most once per
`triggerCheckIntervalTicks` regardless of how many qualifying events happened in that window.

### Change stat effects per stage
Edit `<stages>` in the `HediffDef` in `MMSFP_HauntsGood.xml`. The Awakened stage
(minSeverity 0.67) is the right place for strong or unique bonuses that reward commitment.

### Change the awakening gene bonus
Edit the corresponding `GeneDef` in `MMSFP_HauntGenes.xml`. Note that this is a permanent
gene — changes affect all pawns who have already awakened too (until next save reload).

### Change the archetype (affects social interactions)
Edit `<archetype>` on `HediffCompProperties_Haunt` in `MMSFP_HauntsGood.xml`. Archetypes
control which interaction from `MMSFP_HauntInteractions.xml` fires when two haunted pawns
socialise. Available archetypes: `MSS_FP_Archetype_Aggressive`, `MSS_FP_Archetype_Trickster`,
`MSS_FP_Archetype_Brooding`.

### Add a special interaction between two specific haunts
Add a new `HauntInteractionDef` to `MMSFP_HauntInteractions.xml` in the iconic pair overrides
section. Set both `<hauntDefA>` and `<hauntDefB>` — order does not matter, the system
sorts them. Iconic pair overrides are checked before archetype interactions.

### Adjust poltergeist event frequency or add a new event type
Edit `<weight>` and `<minSeverity>` on existing entries in `MMSFP_HauntEvents.xml`.
To add a new event, create a new `HauntEventDef` entry with a `workerClass` that extends
`MSSFP.Events.HauntEventWorker` and implements `Execute(Pawn hauntedPawn, Map map)`.

---

## Dynamic Colonist Haunts — How It Works

Enabled via the `Enable dynamic grave haunts` mod setting. Independent of the Echo system.

### Assignment

`HauntedMapComponent` uses the same periodic check as named haunts but gates on
`EnableGraveHaunts` instead of `EnableEcho`. The pawn pool is limited to humanlike colonists
not already carrying `MSS_FP_PawnDisplayer` and not HauntResistant.

When a colonist is selected, `HauntProfileBuilder.TryBuild(spirit)` is called on the corpse's
inner pawn. The builder:
1. Skips if the dead pawn's name matches a named haunt source (Grignr, Frog, Kekvit, Oskar,
   Phil, Jade) — the named haunt system handles those
2. Finds the dead colonist's highest non-disabled skill
3. Looks up the matching `HauntSkillMappingDef` in `MMSFP_HauntSkillMappings.xml`
4. Computes `scaleFactor = Clamp01((skillLevel - 8) / 12f)` — skill 8 = 0%, skill 20 = 100%
5. If no skill reaches level 8, uses a fallback `scaleFactor` of 0.1 (restless spirit identity)
6. Copies the mapping's stat offsets into parallel lists (`List<StatDef>` + `List<float>`) for
   save-safe serialization — `StatModifier` is not `IExposable`

The resulting `HauntProfile` is set on the `HediffComp_DynamicHaunt` companion comp on
`MSS_FP_PawnDisplayer`. It is serialized once and **never re-derived** — even if the corpse
or grave is later destroyed.

### Skill → Archetype Mapping

Defined per-skill in `MMSFP_HauntSkillMappings.xml` via `HauntSkillMappingDef`:

| Skills | Archetype | Awakening Gene Category |
|--------|-----------|------------------------|
| Melee, Shooting | Aggressive | Combat |
| Animals | Aggressive | Wanderer |
| Social, Artistic | Trickster | Social |
| Crafting | Trickster | Worker |
| Medicine, Intellectual | Brooding | Scholar |
| Plants, Cooking | Brooding | Wanderer |
| Construction, Mining | Brooding | Worker |

### Stat Effects

`HediffComp_DynamicHaunt` does not use XML `<stages>`. Instead, a Harmony postfix on
`StatWorker.GetValueUnfinalized` reads `HauntProfile.GetStatOffset(stat, severity)` for
any pawn carrying a `HediffComp_DynamicHaunt`.

Stage scaling mirrors named haunts: Whisper (≤0.33) = 33%, Presence (≤0.66) = 67%,
Awakened (>0.66) = 100% of `statValue × scaleFactor`.

### Progression

`HediffComp_DynamicHaunt` fully owns severity for dynamic haunts:
- `MSS_FP_PawnDisplayer` has no `SeverityPerDay` comp — dynamic haunt drives it entirely
- Passive growth: 0.04 severity/day
- Trigger: checks `HauntProfile.triggerRecordDef` every 2500 ticks; fires += 0.02 on delta
- Regression: -0.015/day after 5 days without a trigger

### Awakening Gene

When severity reaches ≥ 0.67 for the first time, one of five generic awakening genes is
granted (weaker than named haunt genes — dynamic haunts are emergent, not authored):

| Gene | Skills |
|------|--------|
| `MSS_FP_MemoryAwakening_Combat` | Melee, Shooting |
| `MSS_FP_MemoryAwakening_Social` | Social, Artistic |
| `MSS_FP_MemoryAwakening_Scholar` | Medicine, Intellectual |
| `MSS_FP_MemoryAwakening_Worker` | Crafting, Construction, Mining |
| `MSS_FP_MemoryAwakening_Wanderer` | Animals, Plants, Cooking (fallback) |

### Interaction System

Dynamic haunts participate in the archetype interaction system via `GetEffectiveArchetype()`,
which checks `HediffComp_DynamicHaunt.Profile.archetype` when `Props.archetype` is null.
This means a pawn haunted by a dead melee colonist (Aggressive archetype) will clash with
a pawn haunted by a dead crafter's ghost (Trickster archetype) using the same interaction
defs as named haunts.

### Adding a New Skill Mapping

To change stat effects for a specific skill, edit the corresponding entry in
`MMSFP_HauntSkillMappings.xml`. Stat values represent full effect at skill level 20,
Awakened severity — `HauntProfileBuilder` scales down from there.

Valid `triggerRecord` values: `KillsHumanlikes`, `KillsAnimals`, `DamageDealt`,
`DamageTaken`, `PrisonersRecruited`, `AnimalsTamed`, `ThingsCrafted`,
`OperationsPerformed`, `ThingsConstructed`, `CellsMined`, `MealsCooked`,
`PlantsSown`, `ResearchPointsResearched`.

---

## Echo System — How It Works

The echo (`MSS_FP_Echo`) is a single hediff that a pawn acquires when a Body Hop ability
is used on them. It uses `HediffComp_Echo` (which extends `HediffComp_Haunt`), giving it
the same ghost rendering infrastructure as named haunts.

### Accumulating history

Each time the echo hops via `CompAbilityEffect_BodyHopImproved`, a `PawnInfo` entry is
recorded on the new host:

| Field | Source |
|-------|--------|
| `name` | Previous host's name |
| `bestSkill` | Highest skill of the previous host |
| `skillOffset` | Skill level converted to a boost value |
| `passedTraits` | Selected traits from the previous host |
| `swapTick` | Game tick when the hop occurred |
| `texPath` | Path to a saved portrait texture for the previous host |

### Skill boosts

All entries in the echo's `pawns` list contribute skill boosts to the current host.
These are applied passively via `SkillRecord_Patch` reading from `HauntsCache`. The
`Gene_SpiritHost` gene doubles all boosts. There is no cap — a long-lived echo with many
hops can grant substantial bonuses across multiple skills.

### Trait passthrough

Traits in each `PawnInfo.passedTraits` are applied to the current host via
`story.traits.GainTrait()` on hop, and removed via `RemoveTrait()` when the echo departs.

### The Spirit inspector tab

`ITab_Echo` shows the full hop history — one row per `PawnInfo` entry with portrait,
name, skill boost, passed traits, and the date of the hop formatted as
"N of Season, Year". The tab is only visible when the echo has at least one history entry.

---

## Susceptibility Genes

These genes modify how both systems interact with a pawn. All three are in the
`Haunts` gene category in `MMSFP_HauntGenes.xml`.

| Gene | Effect | Metabolism |
|------|--------|------------|
| `MSS_FP_Gene_HauntSensitive` | 1.5× chance to be chosen for haunt assignment; haunt progression 2× faster; +5 mood when sensing a nearby haunted pawn | −1 |
| `MSS_FP_Gene_HauntResistant` | Excluded from haunt assignment pool entirely; haunt progression 0.5× | +1 |
| `MSS_FP_Gene_SpiritHost` | Echo skill boosts doubled; +0.2 mental break threshold (strain of hosting) | −2 |

HauntSensitive and HauntResistant are mutually exclusive by design — a pawn should not
have both, but the system handles it gracefully (multipliers stack, so 2× × 0.5× = 1×).
