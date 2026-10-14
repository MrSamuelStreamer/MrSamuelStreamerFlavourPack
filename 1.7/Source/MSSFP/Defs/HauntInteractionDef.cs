using RimWorld;
using Verse;

namespace MSSFP.Defs;

/// <summary>
/// Defines the effect of a haunt interaction — either an archetype-pair reaction
/// or an iconic pair override. Defined in XML; evaluated by HauntInteractionHandler
/// whenever a social interaction occurs between two haunted pawns.
///
/// Lookup priority (highest first):
///   1. Iconic pair override  (hauntDefA + hauntDefB both set)
///   2. Archetype pair        (archetypeA + archetypeB set)
/// Same-haunt-def Amplification is handled in code and does not use this def.
/// </summary>
public class HauntInteractionDef : Def
{
    // --- Matching ---

    /// <summary>For iconic pair overrides: first haunt in the pair (order-independent).</summary>
    public HediffDef hauntDefA;

    /// <summary>For iconic pair overrides: second haunt in the pair (order-independent).</summary>
    public HediffDef hauntDefB;

    /// <summary>For archetype interactions: archetype of pawn A (order-independent).</summary>
    public HauntArchetypeDef archetypeA;

    /// <summary>For archetype interactions: archetype of pawn B (order-independent).</summary>
    public HauntArchetypeDef archetypeB;

    /// <summary>
    /// Chance this interaction fires each time two haunted pawns have a social
    /// interaction. Applied after the pair/archetype match.
    /// </summary>
    public float chancePerSocialInteraction = 0.3f;

    // --- Mood effects ---

    /// <summary>Thought given to the pawn whose haunt matches hauntDefA / archetypeA.</summary>
    public ThoughtDef thoughtForA;

    /// <summary>Thought given to the pawn whose haunt matches hauntDefB / archetypeB.</summary>
    public ThoughtDef thoughtForB;

    /// <summary>Thought given to all colonist pawns within bystanderRadius of either pawn.</summary>
    public ThoughtDef thoughtForBystanders;

    /// <summary>Cell radius around either haunted pawn in which bystanders receive the thought.</summary>
    public float bystanderRadius = 0f;

    // --- Severity effects ---

    /// <summary>Added to the severity of the haunt on pawn A.</summary>
    public float severityDeltaA = 0f;

    /// <summary>Added to the severity of the haunt on pawn B.</summary>
    public float severityDeltaB = 0f;

    // --- Temporary stat hediffs ---

    /// <summary>Hediff applied to pawn A (e.g. short-duration melee buff for Clash).</summary>
    public HediffDef tempHediffForA;

    /// <summary>Hediff applied to pawn B.</summary>
    public HediffDef tempHediffForB;

    // --- Special effects ---

    /// <summary>
    /// If true, a random item within 5 cells of either pawn is teleported 2-3 cells
    /// in a random direction. Used by the Trickster+Trickster Mischief interaction.
    /// </summary>
    public bool teleportNearbyItem = false;
}
