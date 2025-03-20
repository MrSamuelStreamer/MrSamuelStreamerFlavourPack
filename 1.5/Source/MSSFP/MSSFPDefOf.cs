using MSSFP.Genes;
using MSSFP.Thoughts;
using RimWorld;
using Verse;

// ReSharper disable UnassignedReadonlyField

namespace MSSFP;

[DefOf]
public static class MSSFPDefOf
{
    public static readonly ThingDef MSSFP_Frogge;
    public static readonly PawnKindDef MSSFP_Jrogge;
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

    public static readonly ResearchProjectDef MSS_FroggeLeapResearch;

    public static readonly IncidentDef MSS_FroggeResearch;

    public static readonly AbilityDef MSS_FP_WorldLeap;

    public static readonly GeneClassificationDef MSSFP_GoodGenes;
    public static readonly GeneClassificationDef MSSFP_BadGenes;
    public static readonly GeneClassificationDef MSSFP_NeutralGenes;

    public static readonly HediffDef MSSFP_TouchedGrass;

    public static readonly GeneDef MSSFP_Illiterate;

    public static readonly BodyModCategoryDef MSSFP_NaturalMods;
    public static readonly PawnKindDef MSSFP_BabyCritter;
    public static readonly ThoughtDef MSSFP_BabyCannonWTF;

    public static readonly ThingSetMakerDef MSSFP_Construction;

    public static readonly IncidentDef MSS_LoversAdvance;

    public static readonly ThingDef MSSFP_ConstructionOffice;

    public static readonly HediffDef MSS_FP_PossessionHaunt;
    public static readonly ThoughtDef MSS_FP_PossessedThought;

    public static readonly ThingDef MSSFP_HauntedThingFlyer;
    public static readonly ThingDef MSSFP_HauntedThingTargetFlyer;

    static MSSFPDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPDefOf));
}
