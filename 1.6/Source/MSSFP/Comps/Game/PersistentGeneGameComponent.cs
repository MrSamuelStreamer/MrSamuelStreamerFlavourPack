using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Comps.Game;

public class PersistentGeneGameComponent(Verse.Game game) : GameComponent
{
    public class RespawnContext : IExposable
    {
        public RespawnContext() { }

        public RespawnContext(Pawn pawn, Verse.Map map, int tick)
        {
            this.pawn = pawn;
            this.map = map;
            this.tick = tick;
        }

        public int tick;
        public Pawn pawn;
        public Verse.Map map;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tick, "tick");
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_References.Look(ref map, "map");
        }
    }

    public static IntRange RespawnDelayRange = new(GenDate.TicksPerDay, GenDate.TicksPerDay * 2);

    public Verse.Game Game { get; } = game;

    public List<Pawn> PersistentPawns = [];
    public List<RespawnContext> RespawnJobs = [];

    public void RegisterPersistentPawn(Pawn pawn)
    {
        PersistentPawns.Add(pawn);
    }

    public void UnregisterPersistentPawn(Pawn pawn)
    {
        PersistentPawns.Remove(pawn);
    }

    public void Notify_PawnDied(Pawn pawn)
    {
        RespawnContext ctx = new RespawnContext(
            pawn,
            pawn.Map,
            Find.TickManager.TicksGame + RespawnDelayRange.RandomInRange
        );
        RespawnJobs.Add(ctx);
    }

    public override void GameComponentTick()
    {
        if (Find.TickManager.TicksGame % 600 == 0)
        {
            List<RespawnContext> valid = RespawnJobs
                .Where(j => j.tick < Find.TickManager.TicksGame)
                .ToList();
            ResurrectionParams parms = new() { dontSpawn = true };

            foreach (RespawnContext respawnContext in valid)
            {
                if (ResurrectionUtility.TryResurrect(respawnContext.pawn, parms))
                {
                    IncidentParms p = new() { target = respawnContext.map };
                    PawnsArrivalModeDefOf.EdgeWalkIn.Worker.TryResolveRaidSpawnCenter(p);
                    PawnsArrivalModeDefOf.EdgeWalkIn.Worker.Arrive([respawnContext.pawn], p);

                    if (respawnContext.pawn.Corpse != null)
                        respawnContext.pawn.Corpse.Destroy(DestroyMode.Vanish);
                }
            }

            RespawnJobs.RemoveAll(j => valid.Contains(j));
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref PersistentPawns, "PersistentPawns", LookMode.Reference);
        Scribe_Collections.Look(ref RespawnJobs, "RespawnJobs", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            PersistentPawns ??= [];
            RespawnJobs ??= [];
        }
    }
}
