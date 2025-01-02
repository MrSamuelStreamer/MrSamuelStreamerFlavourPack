using RimWorld;
using Verse;
// ReSharper disable UnassignedReadonlyField

namespace MSSFP;

[DefOf]
public static class MSSFPDefOf
{
    public static readonly ThingDef MSSFP_Frogge;
    public static readonly HediffDef MSS_FP_FroggeHaunt;
    public static readonly HediffDef MSS_FP_PawnDisplayer;
    public static readonly PreceptDef MSS_FP_IdeoRole_FroggeWarrior;
    public static readonly FleckDef PsycastPsychicEffect;
    public static readonly ThingDef MSSFP_Plant_TreeFroganlen;

    public static readonly HediffDef MSS_FP_PawnDisplayerPossession;

    public static readonly IncidentDef MSSFP_RaidEnemy_Skylantern;

    public static readonly HediffDef Burn;

    static MSSFPDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPDefOf));
}
