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
        haveChecked = map.ParentFaction.IsPlayer;
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if(!haveChecked)
        {
            if (map.mapPawns.AllPawns.Any(pawn => pawn.Faction == map.ParentFaction && map.ParentFaction.leader == pawn))
            {
                Letter letter = LetterMaker.MakeLetter("MSSFP_LeaderFoundLabel".Translate(), "MSSFP_LeaderFoundText".Translate(map.ParentFaction.leader.Named("LEADER")), map.ParentFaction.HostileTo(Faction.OfPlayer) ? LetterDefOf.ThreatBig: LetterDefOf.NeutralEvent);
                letter.lookTargets = new LookTargets([map.ParentFaction.leader]);
                Find.LetterStack.ReceiveLetter(letter);
            }
        }
    }
}
