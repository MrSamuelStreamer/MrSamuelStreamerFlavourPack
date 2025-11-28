using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class FactionLeaderMapComponent(Verse.Map map) : MapComponent(map)
{
    public bool shouldCheck = false;

    public List<Pawn> SeenLeaders;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref SeenLeaders, "SeenLeaders", LookMode.Reference);
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        // don't notify for player maps.
        Faction faction = map.ParentFaction ?? map.Parent.Faction;

        shouldCheck = faction == null || faction.IsPlayer;
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (shouldCheck)
        {
            return;
        }

        SeenLeaders ??= [];

        if (map?.mapPawns == null || map.mapPawns.AllPawns.NullOrEmpty())
        {
            return;
        }

        Faction faction = map.ParentFaction ?? map.Parent.Faction;
        if (faction == null) return;

        if (!map.mapPawns.AllPawns.Any(pawn => pawn.Faction == faction && faction.leader == pawn))
        {
            return;
        }

        if(SeenLeaders.Contains(faction.leader)) return;

        SeenLeaders.Add(faction.leader);

        Letter letter = LetterMaker.MakeLetter("MSSFP_LeaderFoundLabel".Translate(), "MSSFP_LeaderFoundText".Translate(faction.leader.Named("LEADER")), faction.HostileTo(Faction.OfPlayer) ? LetterDefOf.ThreatBig: LetterDefOf.NeutralEvent);
        letter.lookTargets = new LookTargets([faction.leader]);
        Find.LetterStack.ReceiveLetter(letter);
    }
}
