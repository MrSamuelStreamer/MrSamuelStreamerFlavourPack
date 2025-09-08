using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP.Pawns
{
    public class DynamicPawnStorage : IExposable
    {
        private string originalPawnName;
        private List<Apparel> storedApparel;
        private ThingWithComps storedWeapon;
        private Pawn storedPawn;

        public bool HasStoredPawn => storedPawn != null;
        public string OriginalPawnName => originalPawnName;

        public bool StorePawn(Pawn pawn, DestroyMode destroyMode = DestroyMode.Vanish)
        {
            if (pawn == null || pawn.Destroyed)
                return false;

            try
            {
                originalPawnName = pawn.NameShortColored;
                storedPawn = pawn;
                StoreApparelItems(pawn);

                if (pawn.Map != null)
                    pawn.DeSpawn();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"MSSFP: Failed to store pawn {pawn?.NameShortColored}: {ex.Message}");
                return false;
            }
        }

        private void StoreApparelItems(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                storedApparel = new List<Apparel>();
                foreach (var apparel in pawn.apparel.WornApparel.ToList())
                {
                    if (apparel.def != null)
                    {
                        pawn.apparel.Remove(apparel);
                        storedApparel.Add(apparel);
                    }
                }
            }

            if (pawn.equipment?.Primary != null)
            {
                storedWeapon = pawn.equipment.Primary;
                pawn.equipment.Remove(pawn.equipment.Primary);
            }
        }

        private void ApplyStoredApparel(Pawn pawn)
        {
            if (pawn.apparel != null)
            {
                var wornApparel = pawn.apparel.WornApparel.ToList();
                foreach (var apparel in wornApparel)
                {
                    pawn.apparel.Remove(apparel);
                    apparel.Destroy();
                }
            }

            if (storedApparel?.Count > 0 && pawn.apparel != null)
            {
                foreach (var apparel in storedApparel)
                {
                    if (apparel?.def != null)
                    {
                        pawn.apparel.Wear(apparel);
                    }
                }
            }

            if (storedWeapon != null && pawn.equipment != null)
            {
                pawn.equipment.AddEquipment(storedWeapon);
            }
        }

        public Pawn RestorePawn(IntVec3 position, Map map, Faction factionOverride = null)
        {
            if (!HasStoredPawn || map == null || storedPawn == null)
                return null;

            try
            {
                var restoredPawn = storedPawn;
                restoredPawn.SetPositionDirect(position);

                if (GenPlace.TryPlaceThing(restoredPawn, position, map, ThingPlaceMode.Direct))
                {
                    ApplyStoredApparel(restoredPawn);
                    ClearStorage();
                    return restoredPawn;
                }
                else
                {
                    Log.Error($"MSSFP: Failed to place restored pawn on map");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"MSSFP: Failed to restore pawn {OriginalPawnName}: {ex.Message}");
                return null;
            }
        }

        public Pawn RestorePawnFromSign(Thing sign)
        {
            if (sign?.Map == null)
                return null;
            return RestorePawn(sign.Position, sign.Map);
        }

        public void ClearStorage()
        {
            originalPawnName = null;
            storedApparel = null;
            storedWeapon = null;
            storedPawn = null;
        }

        public string GetStorageDescription()
        {
            return HasStoredPawn
                ? $"Contains transformed pawn: {originalPawnName}"
                : "No pawn stored";
        }

        public bool IsStorageValid()
        {
            return HasStoredPawn && storedPawn != null;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref originalPawnName, "originalPawnName");
            Scribe_Collections.Look(ref storedApparel, "storedApparel", LookMode.Deep);
            Scribe_Deep.Look(ref storedWeapon, "storedWeapon");
            Scribe_Deep.Look(ref storedPawn, "storedPawn");
        }
    }
}
