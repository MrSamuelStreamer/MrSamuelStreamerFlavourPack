using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Gravship;

/// <summary>Good/bad/neutral bucket — used only for documentation and weight-balancing.</summary>
public enum OrbEventFlavour
{
    Bad,
    Neutral,
    Good,
}

/// <summary>
/// One rare "the route-calculating AI did something" event, fired on an orb-assisted gravship
/// landing (see <see cref="MSSFP.HarmonyPatches.GravshipController_LandingEnded_OrbEvents_Patch"/>).
///
/// Subclasses are discovered by reflection across all MSSFP* assemblies (see
/// <see cref="OrbJumpEventManager"/>), so the optional MSSFP.VGE assembly can contribute extra
/// events without the core assembly referencing Vanilla Gravship Expanded.
///
/// All effects land on the DESTINATION map (<c>engine.Map</c>) — the ship, its buildings, and its
/// colonists have already been placed there by the time the host postfix runs.
/// </summary>
public abstract class OrbJumpEvent
{
    /// <summary>Stable key used for logging / dev display.</summary>
    public abstract string Label { get; }

    /// <summary>Relative selection weight within the pool. Default 1.</summary>
    public virtual float Weight => 1f;

    public abstract OrbEventFlavour Flavour { get; }

    /// <summary>Run the effect. Implementations MUST NOT throw — the caller guards, but be tidy.</summary>
    public abstract void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb);

    // ── Shared helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Buildings that belong to the gravship: anything standing on the engine's connected
    /// substructure. Scoping to substructure cells guarantees we never touch destination-map
    /// natives. Distinct, spawned, on the destination map.
    /// </summary>
    protected static List<Building> ShipBuildings(Building_GravEngine engine)
    {
        List<Building> result = new();
        Map map = engine.Map;
        if (map == null)
            return result;
        HashSet<Building> seen = new();
        foreach (IntVec3 cell in engine.AllConnectedSubstructureNoRegen)
        {
            if (!cell.InBounds(map))
                continue;
            foreach (Thing t in cell.GetThingList(map))
            {
                if (t is Building b && b.Spawned && seen.Add(b))
                    result.Add(b);
            }
        }
        return result;
    }

    /// <summary>Player humanlike colonists that travelled on the ship and are still alive.</summary>
    protected static List<Pawn> AboardColonists(RimWorld.Planet.Gravship gravship)
    {
        return gravship
            .Pawns.Where(p =>
                p != null
                && !p.Dead
                && p.RaceProps != null
                && p.RaceProps.Humanlike
                && p.Faction == Faction.OfPlayer
            )
            .ToList();
    }

    /// <summary>
    /// Damage <paramref name="thing"/> without ever destroying it — clamps the dealt amount so at
    /// least 1 hit point remains. No-op on things with no hit points (ethereal, etc.).
    /// </summary>
    protected static void DamageNonDestructive(Thing thing, DamageDef def, float amount, Thing instigator)
    {
        if (thing == null || thing.Destroyed || !thing.def.useHitPoints || thing.MaxHitPoints <= 0)
            return;
        int max = thing.HitPoints - 1;
        if (max <= 0)
            return;
        int dealt = Mathf.Clamp(Mathf.RoundToInt(amount), 1, max);
        thing.TakeDamage(new DamageInfo(def, dealt, 0f, -1f, instigator));
    }

    protected static void SendLetter(string labelKey, string textKey, LetterDef letterDef, Building_GravEngine engine, CompTrueAICore orb)
    {
        string persona = orb?.activePersonality?.LabelCap ?? "AI";
        Find.LetterStack.ReceiveLetter(
            labelKey.Translate(),
            textKey.Translate(persona, engine.RenamableLabel),
            letterDef,
            new LookTargets(engine)
        );
    }

    protected static void SendMessage(string textKey, MessageTypeDef type, Building_GravEngine engine, CompTrueAICore orb)
    {
        string persona = orb?.activePersonality?.LabelCap ?? "AI";
        Messages.Message(textKey.Translate(persona, engine.RenamableLabel), engine, type, historical: false);
    }
}
