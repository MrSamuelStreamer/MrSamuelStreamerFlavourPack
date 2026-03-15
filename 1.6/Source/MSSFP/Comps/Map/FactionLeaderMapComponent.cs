using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

// Sends a one-time "leader spotted" letter the first time a given faction
// leader appears on this map. Using a MapComponent (not GameComponent) gives
// per-map granularity: the same leader appearing on a different map — their
// home settlement, then the player's colony during a raid — triggers a
// notification each time, since each context is independently meaningful.
//
// NotifiedLeaders resets if the map is unloaded (settlement destroyed), which
// is intentional: a leader appearing on a brand-new map is worth re-notifying.
public class FactionLeaderMapComponent(Verse.Map map) : MapComponent(map)
{
    public List<Pawn> NotifiedLeaders;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref NotifiedLeaders, "NotifiedLeaders", LookMode.Reference);
        NotifiedLeaders?.RemoveAll(p => p == null);
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame % GenTicks.TickRareInterval != 0) return;
        if (map?.mapPawns == null) return;

        NotifiedLeaders ??= [];

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.Faction == null || pawn.Faction.IsPlayer) continue;
            if (pawn.Faction.leader != pawn) continue;
            if (NotifiedLeaders.Contains(pawn)) continue;

            NotifiedLeaders.Add(pawn);

            LetterDef letterDef = pawn.Faction.HostileTo(Faction.OfPlayer)
                ? LetterDefOf.ThreatBig
                : LetterDefOf.NeutralEvent;
            Letter letter = LetterMaker.MakeLetter(
                "MSSFP_LeaderFoundLabel".Translate(),
                "MSSFP_LeaderFoundText".Translate(pawn.Named("LEADER")),
                letterDef
            );
            letter.lookTargets = new LookTargets([pawn]);
            Find.LetterStack.ReceiveLetter(letter);
        }
    }
}
