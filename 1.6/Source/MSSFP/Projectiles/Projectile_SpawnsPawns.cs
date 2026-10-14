using RimWorld;
using Verse;

namespace MSSFP.Projectiles;

public class Projectile_SpawnsPawns : Projectile
{
    protected override void Impact(Thing hitThing, bool blockedByShield = false)
    {
        Map map = Map;
        int count = 1;
        ProjectileModExtension modExt = def.GetModExtension<ProjectileModExtension>();
        if (modExt != null)
            count = modExt.spawnCount.RandomInRange;

        for (int i = 0; i < count; i++)
        {
            IntVec3 loc = Position;
            if (def.projectile.tryAdjacentFreeSpaces && Position.GetFirstBuilding(map) != null)
            {
                foreach (IntVec3 cell in GenAdjFast.AdjacentCells8Way(Position))
                {
                    if (cell.GetFirstBuilding(map) == null && cell.Standable(map))
                    {
                        loc = cell;
                        break;
                    }
                }
            }

            PawnKindDef spawnsPawnKind = def.projectile.spawnsPawnKind;
            Pawn pawn = PawnGenerator.GeneratePawn(spawnsPawnKind, launcher?.Faction);
            GenSpawn.Spawn(pawn, loc, map, WipeMode.Vanish);

            if ((modExt?.angry ?? false) && hitThing is Pawn target)
            {
                if (pawn.MentalState == null)
                {
                    pawn.mindState?.mentalStateHandler.TryStartMentalState(
                        MentalStateDefOf.Manhunter,
                        otherPawn: target
                    );
                }

                pawn.MentalState?.ForceHostileTo(target);
            }
        }

        base.Impact(hitThing, blockedByShield);
    }
}
