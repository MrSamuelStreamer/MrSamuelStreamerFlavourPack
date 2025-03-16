using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Questing;

public class QuestPart_Perganante : QuestPart
{
    public string inSignal;
    public MapParent mapParent;

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        if (signal.tag != inSignal)
            return;

        IEnumerable<Pawn> allRaiders = null;

        if (mapParent != null)
        {
            Map map = mapParent.Map;
            if (map == null)
            {
                Log.Error("Cannot resolve map for QuestPart_Perganante.mapParent!");
                return;
            }
            allRaiders = map.mapPawns.FreeColonistsSpawned;
        }
        if (allRaiders == null)
            return;

        foreach (Pawn pawn in allRaiders)
        {
            if (pawn.health.hediffSet.HasHediff(HediffDefOf.PregnantHuman))
                continue;

            Pawn spouse = pawn.GetFirstSpouse();

            if (spouse == null)
                continue;

            Hediff_Pregnant preg = pawn.health.AddHediff(HediffDefOf.PregnantHuman) as Hediff_Pregnant;
            preg!.SetParents(pawn, spouse, PregnancyUtility.GetInheritedGeneSet(spouse, pawn));
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref mapParent, "mapParent");
    }
}
