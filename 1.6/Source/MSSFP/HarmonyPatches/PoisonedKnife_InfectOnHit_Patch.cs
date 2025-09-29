using HarmonyLib;
using RimWorld;
using Verse;

namespace MSSFP.HarmonyPatches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
    public static class PoisonedKnife_InfectOnHit_Patch
    {
        public static void Postfix(
            Thing __instance,
            DamageInfo dinfo,
            DamageWorker.DamageResult __result
        )
        {
            if (__instance is not Pawn pawn || pawn.Dead || !pawn.RaceProps.IsFlesh)
                return;

            if (dinfo.Weapon?.defName != "MSS_PoisonedKnife")
                return;

            if (__result?.totalDamageDealt <= 0 || !CausedBleeding(__result))
                return;

            if (!Rand.Chance(0.4f))
                return;

            var existing = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.WoundInfection);
            if (existing != null)
            {
                existing.Severity = System.Math.Min(existing.Severity + 0.1f, 0.95f);
            }
            else
            {
                var newHediff = HediffMaker.MakeHediff(HediffDefOf.WoundInfection, pawn);
                newHediff.Severity = Rand.Range(0.15f, 0.35f);
                pawn.health.AddHediff(newHediff);
            }
        }

        private static bool CausedBleeding(DamageWorker.DamageResult damageResult)
        {
            foreach (var hediff in damageResult.hediffs)
            {
                if (hediff is Hediff_Injury injury && injury.Bleeding)
                    return true;
            }
            return false;
        }
    }
}
