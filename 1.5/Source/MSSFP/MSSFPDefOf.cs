using RimWorld;
using Verse;
// ReSharper disable UnassignedReadonlyField

namespace MSSFP;

[DefOf]
public static class MSSFPDefOf
{
    public static readonly ThingDef MSSFP_Frogge;
    public static readonly ThingDef MSS_FP_Froggomancer;
    public static readonly HediffDef MSS_FP_FroggeHaunt;
    public static readonly HediffDef MSS_FP_PawnDisplayer;
    public static readonly PreceptDef MSS_FP_IdeoRole_FroggeWarrior;
    public static readonly FleckDef PsycastPsychicEffect;
    public static readonly ThingDef MSSFP_Plant_TreeFroganlen;

    public static readonly HediffDef MSS_FP_PawnDisplayerPossession;

    public static readonly IncidentDef MSSFP_RaidEnemy_Skylantern;

    public static readonly HediffDef Burn;

    public static readonly ThoughtDef MSSFP_Marked;

    public static readonly IncidentDef MSS_FroggomancerRescue;
    public static readonly IncidentDef MSSFP_Lovers_Retreat;

    public static readonly TaleDef MSSFP_Lovers_Retreat_Tale;

    public static readonly JobDef MSSFP_GoToThen;

    static MSSFPDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPDefOf));
}
