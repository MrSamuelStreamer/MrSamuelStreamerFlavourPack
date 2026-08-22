using System.Collections.Generic;
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
        if (PawnsToBeam.Count == 0)
            return;

        int now = Find.TickManager.TicksGame;
        for (int i = PawnsToBeam.Count - 1; i >= 0; i--)
        {
            PawnBeamer beamer = PawnsToBeam[i];
            if (beamer.ticks > now)
                continue;

            PawnsToBeam.RemoveAt(i);

            Pawn pawn = beamer.pawn;
            if (pawn == null || pawn.Destroyed || !pawn.Spawned)
                continue;

            SendLetter(pawn);
            Effecter e = EffecterDefOf.Skip_ExitNoDelay.Spawn(pawn.Position, map);
            e.Trigger(
                new TargetInfo(pawn.Position, map),
                new TargetInfo(pawn.Position, map)
            );
            pawn.DeSpawn();
            Find.WorldPawns.PassToWorld(pawn);
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
