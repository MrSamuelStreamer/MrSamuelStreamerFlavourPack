using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class FactionLeaderMapComponent(Verse.Map map) : MapComponent(map)
{
    public bool haveChecked = false;

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        // don't notify for player maps.
        Faction faction = map.ParentFaction ?? map.Parent.Faction;

        haveChecked = faction == null || faction.IsPlayer;
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (haveChecked)
        {
            return;
        }

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

        Letter letter = LetterMaker.MakeLetter("MSSFP_LeaderFoundLabel".Translate(), "MSSFP_LeaderFoundText".Translate(faction.leader.Named("LEADER")), faction.HostileTo(Faction.OfPlayer) ? LetterDefOf.ThreatBig: LetterDefOf.NeutralEvent);
        letter.lookTargets = new LookTargets([faction.leader]);
        Find.LetterStack.ReceiveLetter(letter);
    }
}
