using System.Collections.Generic;
using CoolPsycasts;
using HarmonyLib;
using MSSFP.Hediffs;
using Verse;

namespace MSSFP.CoolerPsycasts;

[HarmonyPatch(typeof(CompAbilityEffect_BodyHop))]
public static class CompAbilityEffect_BodyHop_Patch
{
    public struct OriginalPawnData
    {
        public string texPath;
        public TaggedString name;
    }

    public static Dictionary<Pawn, OriginalPawnData> pawnOriginalNames = new Dictionary<Pawn, OriginalPawnData>();

    [HarmonyPatch(nameof(CompAbilityEffect_BodyHop.Apply))]
    [HarmonyPrefix]
    public static void Apply_Pre(CompAbilityEffect_BodyHop __instance, LocalTargetInfo target)
    {
        Pawn host = target.Thing as Pawn;

        if(host is null) return;

        string texPath = PawnGraphicUtils.SavePawnTexture(host);

        if (pawnOriginalNames.ContainsKey(host)) pawnOriginalNames.Remove(host);
        pawnOriginalNames.Add(host, new OriginalPawnData()
        {
            texPath = texPath,
            name = host.NameFullColored
        });
    }

    [HarmonyPatch(nameof(CompAbilityEffect_BodyHop.Apply))]
    [HarmonyPostfix]
    public static void Apply_Post(CompAbilityEffect_BodyHop __instance, LocalTargetInfo target)
    {
        Pawn host = target.Thing as Pawn;

        if(host is null) return;

        Hediff hediff = host.health.AddHediff(MSSFPDefOf.MSS_FP_PawnDisplayerPossession);

        HediffComp_Haunt haunt = hediff.TryGetComp<HediffComp_Haunt>();

        if(haunt is null) return;
        haunt.TexPath = pawnOriginalNames[host].texPath;
        haunt.name = pawnOriginalNames[host].name;
    }
}
