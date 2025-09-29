using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps
{
    public class CompTargetEffect_PlaceSiegeLadder : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is Building wall)
            {
                if (wall is MSSFP.Buildings.Building_ClimbableWallProxy proxy)
                {
                    Messages.Message(
                        "MSS_SiegeLadder_AlreadyPlaced".Translate(),
                        MessageTypeDefOf.RejectInput,
                        false
                    );
                    return;
                }

                if (!CanClimbOverWall(user, wall))
                {
                    Messages.Message(
                        "MSS_SiegeLadder_FarSideRoofed".Translate(),
                        MessageTypeDefOf.RejectInput,
                        false
                    );
                    return;
                }

                if (HasWallBehind(user, wall))
                {
                    Messages.Message(
                        "Cannot place ladder - there are multiple walls in a row.",
                        MessageTypeDefOf.RejectInput,
                        false
                    );
                    return;
                }

                var proxyWall = new MSSFP.Buildings.Building_ClimbableWallProxy();
                proxyWall.InitializeProxy(wall);
                SwapWallWithProxy(wall, proxyWall);
                proxyWall.MakeClimbable(3600);
            }
        }

        private void SwapWallWithProxy(
            Building originalWall,
            MSSFP.Buildings.Building_ClimbableWallProxy proxy
        )
        {
            var map = originalWall.Map;
            var position = originalWall.Position;

            map.edificeGrid.DeRegister(originalWall);
            map.thingGrid.Deregister(originalWall);
            map.listerBuildings.Remove(originalWall);

            proxy.SetPositionDirect(position);
            proxy.SpawnSetup(map, false);

            map.mapDrawer.MapMeshDirty(position, MapMeshFlagDefOf.Buildings);
            map.mapDrawer.MapMeshDirty(position, MapMeshFlagDefOf.Things);
        }

        public override bool CanApplyOn(Thing target)
        {
            return target is Building building && IsWall(building);
        }

        private bool IsWall(Building building)
        {
            return building.def.IsWall
                || building.def.fillPercent >= 0.99f
                || building.def.blockWind
                || building.def.holdsRoof;
        }

        public bool CanClimbOverWall(Pawn user, Building wall)
        {
            if (wall?.Map == null)
                return false;

            var adjacentCells = GenAdjFast.AdjacentCells8Way(wall.Position).ToList();
            var userCell = user.Position;

            IntVec3 farSide = IntVec3.Invalid;
            foreach (var cell in adjacentCells)
            {
                if (cell.InBounds(wall.Map) && cell.Standable(wall.Map))
                {
                    var direction1 = (wall.Position - userCell).ToVector3();
                    var direction2 = (cell - wall.Position).ToVector3();

                    if (Vector3.Dot(direction1.normalized, direction2.normalized) > 0.5f)
                    {
                        farSide = cell;
                        break;
                    }
                }
            }

            if (farSide == IntVec3.Invalid)
            {
                return false;
            }

            return !wall.Map.roofGrid.Roofed(farSide);
        }

        public bool HasWallBehind(Pawn user, Building wall)
        {
            if (wall?.Map == null)
                return false;

            var userCell = user.Position;
            var wallPosition = wall.Position;

            // Get the direction from user to wall
            var userToWallDirection = (wallPosition - userCell).ToVector3().normalized;

            // Check the cell behind the wall (opposite side from user)
            var behindWallCell = wallPosition + IntVec3Utility.ToIntVec3(userToWallDirection);

            if (!behindWallCell.InBounds(wall.Map))
                return false;

            // Check if there's a wall at the behind position
            var buildingBehind = wall.Map.edificeGrid[behindWallCell];

            return buildingBehind != null && IsWall(buildingBehind);
        }
    }
}
