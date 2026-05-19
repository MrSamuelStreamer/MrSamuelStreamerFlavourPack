using MSSFP.Defs;
using Verse;

namespace MSSFP.Comps;

/// <summary>How chatty an AI core is. Maps to MTB multiplier and letter throttling.</summary>
public enum AIVerbosity
{
    Quiet = 0,
    Normal = 1,
    Loud = 2,
}

public class CompProperties_TrueAICore : CompProperties
{
    /// <summary>Personality the core boots with. May be null — comp will roll one on spawn if so.</summary>
    public AIPersonalityDef defaultPersonality;

    /// <summary>If true, players can swap personalities via a gizmo float menu.</summary>
    public bool showPersonalitySelector = true;

    /// <summary>Mean-time-between chatter rolls in in-game hours (Normal verbosity).</summary>
    public float chatterMtbHours = 4f;

    /// <summary>Default verbosity. Loud halves MTB; Quiet doubles it.</summary>
    public AIVerbosity verbosity = AIVerbosity.Normal;

    /// <summary>Min ticks between back-to-back chatter events on this core. Prevents bursting.</summary>
    public int chatterCooldownTicks = 600;

    /// <summary>Probability per chatter hit that the line escalates to a Letter (suppressed during danger).</summary>
    public float letterChance = 0.02f;

    /// <summary>Hard cap on letters this core can fire per in-game day.</summary>
    public int lettersPerDay = 1;

    /// <summary>
    /// Multiplier applied to <see cref="chatterMtbHours"/> when the parent building has a
    /// <see cref="MSSFP.Holo.CompHoloProjector"/>. Holos channel personality through vanilla
    /// social interactions (<c>socialInitiator</c>), so building-bubble ambient chatter is
    /// suppressed via a longer MTB. Non-holo cores ignore this. Set 1f to opt out.
    /// </summary>
    public float holoChatterMtbMult = 4f;

    /// <summary>
    /// Multiplier applied to the holo-pawn address MTB in
    /// <see cref="MSSFP.Holo.CompHoloProjected"/>. Split from chatter so addresses (more
    /// attention-grabbing motes) can be tuned independently. Non-holo cores ignore.
    /// </summary>
    public float holoAddressMtbMult = 3f;

    /// <summary>
    /// Multiplier applied to <see cref="letterChance"/> when the parent has a holo projector.
    /// Holos rarely fire popup letters — the persona speaks via socials and quiet bubble lines.
    /// Set 1f to opt out, 0f for hard mute.
    /// </summary>
    public float holoLetterMult = 0.05f;

    /// <summary>
    /// Per-day letter cap for holo cores. Combined with <see cref="lettersPerDay"/> via
    /// <c>Math.Min</c> so a chart that lowers the building-wide cap still wins.
    /// </summary>
    public int holoLettersPerDay = 1;

    /// <summary>Stuff list accepted by the "Create AI Art" haul job (e.g. Stoneblocks, WoodLog).</summary>
    public System.Collections.Generic.List<ThingDef> artInputs;

    /// <summary>Resource units consumed per artwork.</summary>
    public int artInputCount = 75;

    /// <summary>ThingDef of the sculpture spawned (must have CompArt + CompQuality). Default: SculptureSmall.</summary>
    public ThingDef artOutputDef;

    public CompProperties_TrueAICore()
    {
        compClass = typeof(CompTrueAICore);
    }
}
