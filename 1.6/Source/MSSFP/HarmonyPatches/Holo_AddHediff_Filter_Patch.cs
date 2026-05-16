using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches
{
    /// <summary>
    /// Prefix on <see cref="Pawn_HealthTracker.AddHediff(Hediff, BodyPartRecord, System.Nullable{DamageInfo}, DamageWorker.DamageResult)"/>
    /// — the canonical overload that the <see cref="HediffDef"/> overload internally calls.
    /// Skipping here drops the constructed <see cref="Hediff"/> on the floor; nothing has been
    /// added to <see cref="HediffSet"/> yet, so no cleanup is required.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
        new[] { typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo?), typeof(DamageWorker.DamageResult) })]
    public static class Holo_AddHediff_Filter_Patch
    {
        public static bool Prefix(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (hediff == null) return true;
            Pawn pawn = __instance?.hediffSet?.pawn;
            if (!MSSFPHoloUtil.IsHolo(pawn)) return true;
            if (HoloHediffPolicy.IsAllowed(pawn, hediff.def)) return true;
            return false;
        }
    }
}
