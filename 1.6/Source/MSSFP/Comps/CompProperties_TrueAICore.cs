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
