using Verse;

namespace MSSFP;

public class Projectile_ExplosiveAndSpawnPawn: Projectile_Explosive
{
    public PawnKindDef kind => DefDatabase<PawnKindDef>.AllDefsListForReading.FirstOrDefault(d => d.defName == "MSSFP_BabyCritter");

    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = Map;
        base.Impact(hitThing, blockedByShield);

        if(kind == null) return;

        IntVec3 loc = Position;

        foreach (IntVec3 c in GenAdjFast.AdjacentCells8Way(Position))
        {
            if (c.GetFirstBuilding(map) == null && c.Standable(map) && Rand.Chance(.75f))
            {
                Pawn p = PawnGenerator.GeneratePawn(kind, Find.FactionManager.OfInsects);

                GenSpawn.Spawn(p, loc, map);
            }
        }

    }
}
