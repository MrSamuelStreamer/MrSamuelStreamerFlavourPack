using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP;

public class Projectile_ExplosiveAndSpawnPawn: Projectile_Explosive
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = Map;
        base.Impact(hitThing, blockedByShield);

        IntVec3 loc = Position;

        foreach (IntVec3 c in GenAdjFast.AdjacentCells8Way(Position))
        {
            if (c.GetFirstBuilding(map) == null && c.Standable(map) && Rand.Chance(.75f))
            {
                Pawn p = PawnGenerator.GeneratePawn(MSSFPDefOf.MSSFP_BabyCritter, Find.FactionManager.OfInsects);

                GenSpawn.Spawn(p, loc, map);
            }
        }

        foreach (Pawn pawn in map.mapPawns.AllHumanlike)
        {
            TryAddMemory(pawn);
        }

    }

    protected virtual void TryAddMemory(Pawn pawn)
    {
        if (pawn.needs?.mood?.thoughts?.memories?.GetFirstMemoryOfDef(MSSFPDefOf.MSSFP_BabyCannonWTF) != null)
            return;
        Thought_Memory newThought = (Thought_Memory) ThoughtMaker.MakeThought(MSSFPDefOf.MSSFP_BabyCannonWTF);
        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(newThought);
    }
}
