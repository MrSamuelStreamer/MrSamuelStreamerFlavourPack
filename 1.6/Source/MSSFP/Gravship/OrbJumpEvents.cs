using System.Collections.Generic;
using System.Linq;
using MSSFP.Comps;
using MSSFP.Comps.Map;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MSSFP.Gravship;

// ── Bad ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Flagship event. The AI hallucinated a hole through the planet and flew the ship into the
/// ground. Widespread non-destructive damage to ship buildings; a few colonists get bruised.
/// </summary>
public class OrbEvent_HoleThroughPlanet : OrbJumpEvent
{
    public override string Label => "HoleThroughPlanet";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Bad;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        foreach (Building b in ShipBuildings(engine))
        {
            // Damage roughly a third to two-thirds of each building's current HP — heavy but never fatal.
            float amount = b.HitPoints * Rand.Range(0.3f, 0.65f);
            DamageNonDestructive(b, DamageDefOf.Crush, amount, engine);
        }

        foreach (Pawn p in AboardColonists(gravship).Where(_ => Rand.Chance(0.5f)))
        {
            if (!p.Dead)
                p.TakeDamage(new DamageInfo(DamageDefOf.Blunt, Rand.Range(3, 8), instigator: engine));
        }

        SendLetter(
            "MSSFP_OrbEvent_HoleThroughPlanet_Label",
            "MSSFP_OrbEvent_HoleThroughPlanet_Text",
            LetterDefOf.NegativeEvent,
            engine,
            orb
        );
    }
}

/// <summary>The AI crashed mid-calculation. Its route-assist goes offline for a few days.</summary>
public class OrbEvent_DivideByZero : OrbJumpEvent
{
    public override string Label => "DivideByZero";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Bad;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        int days = Rand.RangeInclusive(2, 5);
        orb.assistDisabledUntilTick = GenTicks.TicksGame + days * GenDate.TicksPerDay;
        SendLetter(
            "MSSFP_OrbEvent_DivideByZero_Label",
            "MSSFP_OrbEvent_DivideByZero_Text",
            LetterDefOf.NegativeEvent,
            engine,
            orb
        );
    }
}

/// <summary>Routed through a debris field. Light hull dents + scattered scrap near the engine.</summary>
public class OrbEvent_DebrisField : OrbJumpEvent
{
    public override string Label => "DebrisField";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Bad;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        List<Building> buildings = ShipBuildings(engine);
        foreach (Building b in buildings.InRandomOrder().Take(Rand.RangeInclusive(2, 5)))
            DamageNonDestructive(b, DamageDefOf.Crush, Rand.Range(10f, 25f), engine);

        Map map = engine.Map;
        for (int i = 0; i < Rand.RangeInclusive(2, 4); i++)
        {
            Thing slag = ThingMaker.MakeThing(ThingDefOf.ChunkSlagSteel);
            GenPlace.TryPlaceThing(slag, engine.Position, map, ThingPlaceMode.Near);
        }
        Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = Rand.RangeInclusive(20, 60);
        GenPlace.TryPlaceThing(steel, engine.Position, map, ThingPlaceMode.Near);

        SendMessage("MSSFP_OrbEvent_DebrisField_Msg", MessageTypeDefOf.NegativeEvent, engine, orb);
    }
}

/// <summary>A time-dilated route leaves the colonists nauseous. Temporary hediff.</summary>
public class OrbEvent_JumpNausea : OrbJumpEvent
{
    public override string Label => "JumpNausea";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Bad;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        HediffDef nausea = HediffDef.Named("MSSFP_JumpNausea");
        foreach (Pawn p in AboardColonists(gravship))
        {
            if (!p.health.hediffSet.HasHediff(nausea))
                p.health.AddHediff(nausea);
        }
        SendLetter(
            "MSSFP_OrbEvent_JumpNausea_Label",
            "MSSFP_OrbEvent_JumpNausea_Text",
            LetterDefOf.NegativeEvent,
            engine,
            orb
        );
    }
}

// ── Neutral / weird ─────────────────────────────────────────────────────────

/// <summary>
/// The hallucination bled into reality. Tries to fire one poltergeist haunt event (reusing the
/// existing system); the flavour message is sent first so it always lands even if no haunt fires.
/// </summary>
public class OrbEvent_GhostInTheMachine : OrbJumpEvent
{
    public override string Label => "GhostInTheMachine";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Neutral;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        SendMessage("MSSFP_OrbEvent_GhostInTheMachine_Msg", MessageTypeDefOf.NeutralEvent, engine, orb);

        Pawn focus = AboardColonists(gravship).RandomElementWithFallback();
        if (focus == null)
            return;
        // Guarded reuse: if the haunt system isn't ready / has no eligible event, this is a no-op.
        // The manager wraps Fire() in try/catch, so a worker that dislikes a null source can't escape.
        engine.Map?.GetComponent<HauntEventMapComponent>()?.ForceRandomEvent(focus);
    }
}

/// <summary>A gravitic hiccup on arrival briefly knocks the colonists off their feet.</summary>
public class OrbEvent_GravityHiccup : OrbJumpEvent
{
    public override string Label => "GravityHiccup";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Neutral;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        foreach (Pawn p in AboardColonists(gravship))
        {
            if (p.Spawned && p.stances?.stunner != null)
                p.stances.stunner.StunFor(Rand.RangeInclusive(120, 300), engine, addBattleLog: false);
        }
        SendMessage("MSSFP_OrbEvent_GravityHiccup_Msg", MessageTypeDefOf.NeutralEvent, engine, orb);
    }
}

/// <summary>The persona broadcasts a flash of its own dread to the crew. Brief colony-wide mood dip.</summary>
public class OrbEvent_ExistentialBroadcast : OrbJumpEvent
{
    public override string Label => "ExistentialBroadcast";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Neutral;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        ThoughtDef thought = ThoughtDef.Named("MSSFP_ExistentialBroadcast");
        foreach (Pawn p in AboardColonists(gravship))
            p.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);
        SendLetter(
            "MSSFP_OrbEvent_ExistentialBroadcast_Label",
            "MSSFP_OrbEvent_ExistentialBroadcast_Text",
            LetterDefOf.NeutralEvent,
            engine,
            orb
        );
    }
}

// ── Good / jackpot ───────────────────────────────────────────────────────────

/// <summary>The AI found an impossible shortcut. The engine's post-jump cooldown is wiped.</summary>
public class OrbEvent_HallucinatedShortcut : OrbJumpEvent
{
    public override string Label => "HallucinatedShortcut";
    public override float Weight => 1.5f;
    public override OrbEventFlavour Flavour => OrbEventFlavour.Good;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        engine.cooldownCompleteTick = GenTicks.TicksGame;
        SendLetter(
            "MSSFP_OrbEvent_HallucinatedShortcut_Label",
            "MSSFP_OrbEvent_HallucinatedShortcut_Text",
            LetterDefOf.PositiveEvent,
            engine,
            orb
        );
    }
}

/// <summary>The persona solved an open problem en route — a research windfall.</summary>
public class OrbEvent_FlashOfInsight : OrbJumpEvent
{
    public override string Label => "FlashOfInsight";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Good;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        ResearchProjectDef proj = Find.ResearchManager.GetProject();
        if (proj != null)
        {
            float remaining = proj.Cost - Find.ResearchManager.GetProgress(proj);
            float bump = Mathf.Min(remaining, proj.Cost * 0.15f);
            if (bump > 0f)
                Find.ResearchManager.AddProgress(proj, bump);
        }
        else
        {
            // No active project — grant the crew a little Intellectual insight instead.
            foreach (Pawn p in AboardColonists(gravship))
                p.skills?.Learn(SkillDefOf.Intellectual, 3000f, direct: true, ignoreLearnRate: true);
        }
        SendLetter(
            "MSSFP_OrbEvent_FlashOfInsight_Label",
            "MSSFP_OrbEvent_FlashOfInsight_Text",
            LetterDefOf.PositiveEvent,
            engine,
            orb
        );
    }
}

/// <summary>A perfectly smooth burn. Some fuel is recovered and the crew rides easy.</summary>
public class OrbEvent_FlawlessBurn : OrbJumpEvent
{
    public override string Label => "FlawlessBurn";
    public override float Weight => 1.5f;
    public override OrbEventFlavour Flavour => OrbEventFlavour.Good;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        foreach (CompGravshipFacility comp in engine.GravshipComponents)
        {
            if (!comp.Props.providesFuel)
                continue;
            CompRefuelable refuelable = comp.parent.TryGetComp<CompRefuelable>();
            if (refuelable != null)
                refuelable.Refuel(refuelable.Props.fuelCapacity * 0.25f);
        }

        ThoughtDef thought = ThoughtDef.Named("MSSFP_FlawlessBurn");
        foreach (Pawn p in AboardColonists(gravship))
            p.needs?.mood?.thoughts?.memories?.TryGainMemory(thought);

        SendMessage("MSSFP_OrbEvent_FlawlessBurn_Msg", MessageTypeDefOf.PositiveEvent, engine, orb);
    }
}
