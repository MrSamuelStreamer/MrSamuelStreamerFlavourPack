using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Map;

public class TrekBeamerMapComponent(Verse.Map map) : MapComponent(map)
{
    public class PawnBeamer : IExposable
    {
        public Pawn pawn;
        public int ticks;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref ticks, "ticks");
        }
    }

    public List<PawnBeamer> PawnsToBeam = [];

    public void BeamAwayPawn(Pawn pawn, int delay = 600)
    {
        PawnBeamer beamer = new PawnBeamer();
        beamer.pawn = pawn;
        beamer.ticks = Find.TickManager.TicksGame + delay;
        PawnsToBeam.Add(beamer);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref PawnsToBeam, "PawnsToBeam", LookMode.Deep);

        if (PawnsToBeam == null)
            PawnsToBeam = [];
    }

    public override void MapComponentTick()
    {
        if (!MSSFPMod.settings.EnableTrekBeamers)
            return;
        if (PawnsToBeam == null)
            PawnsToBeam = [];
        foreach (
            PawnBeamer beamer in PawnsToBeam
                .Where(key => key.ticks <= Find.TickManager.TicksGame)
                .ToList()
        )
        {
            SendLetter(beamer.pawn);
            Effecter e = EffecterDefOf.Skip_ExitNoDelay.Spawn(beamer.pawn.Position, map);
            e.Trigger(
                new TargetInfo(beamer.pawn.Position, map),
                new TargetInfo(beamer.pawn.Position, map)
            );
            PawnsToBeam.Remove(beamer);
            beamer.pawn.DeSpawn();
            Find.WorldPawns.PassToWorld(beamer.pawn);
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
