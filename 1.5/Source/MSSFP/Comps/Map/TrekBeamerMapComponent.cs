using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class TrekBeamerMapComponent(Verse.Map map) : MapComponent(map)
{
    public Dictionary<int, Pawn> PawnsToBeam = new Dictionary<int, Pawn>();

    public void BeamAwayPawn(Pawn pawn, int delay = 600)
    {
        PawnsToBeam.Add(Find.TickManager.TicksGame + delay, pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref PawnsToBeam, "BeamPawns", LookMode.Value, LookMode.Reference);
    }

    public override void MapComponentTick()
    {
        foreach (int tick in PawnsToBeam.Keys.Where(key => key <= Find.TickManager.TicksGame).ToList())
        {
            Pawn p = PawnsToBeam[tick];
            SendLetter(p);
            Effecter e = EffecterDefOf.Skip_ExitNoDelay.Spawn(p.Position, map);
            e.Trigger(new TargetInfo(p.Position, map), new TargetInfo(p.Position, map));
            PawnsToBeam.Remove(tick);
            p.DeSpawn();
            Find.WorldPawns.PassToWorld(p);
        }
    }

    public void SendLetter(Pawn pawn)
    {
        Letter letter = LetterMaker.MakeLetter(
            "MSSFP_TrekBeamerLetter".Translate(pawn.Named("PAWN")),
            "MSSFP_TrekBeamerLetterText".Translate(pawn.Named("PAWN")),
            LetterDefOf.NeutralEvent,
            new TargetInfo(pawn.Position, map)
        );
        Find.LetterStack.ReceiveLetter(letter);
    }
}
