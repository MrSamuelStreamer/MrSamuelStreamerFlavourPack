﻿using MSSFP.Genes;
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
    public static readonly HediffDef MSS_FP_FroggeHaunt;
    public static readonly HediffDef MSS_FP_PawnDisplayer;
    public static readonly PreceptDef MSS_FP_IdeoRole_FroggeWarrior;
    public static readonly FleckDef PsycastPsychicEffect;

    public static readonly HediffDef MSS_FP_PawnDisplayerPossession;

    public static readonly IncidentDef MSSFP_RaidEnemy_Skylantern;

    public static readonly HediffDef Burn;

    public static readonly ThoughtDef MSSFP_Marked;

    public static readonly IncidentDef MSSFP_Lovers_Retreat;

    public static readonly TaleDef MSSFP_Lovers_Retreat_Tale;

    public static readonly JobDef MSSFP_GoToThen;

    public static readonly ResearchProjectDef MSS_FroggeLeapResearch;
    public static readonly ResearchProjectDef MSS_Oskarian_Technology;

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

    public static readonly RulePackDef MSS_Nonsense;
    public static readonly TaleDef MSSFP_Nonsense_Tale;
    public static readonly ThoughtDef MSSFP_Nonsense_Thought_Bad;
    public static readonly ThoughtDef MSSFP_Nonsense_Thought_Neutral;
    public static readonly ThoughtDef MSSFP_Nonsense_Thought_Good;

    public static readonly HediffDef MSS_Need_GeneStealer_Restless;
    public static readonly HediffDef MSS_Need_GeneStealer_Exhaustion;

    public static readonly XenotypeDef MSSFP_Taff;
    public static readonly FactionDef MSSFP_TaffsFaction;

    public static readonly HediffDef MSS_FP_WellSlept;

    // public static readonly HediffDef MSSFP_Hediff_DRM;

    public static readonly ThingSetMakerDef MSSFP_TrekCharacter;

    public static readonly PawnKindDef MSSFP_TrekCrasher;

    public static readonly QuestScriptDef MSS_TrekPodCrash;

    public static readonly BackstoryDef MSSFP_Trek;

    public static readonly JobDef MSSFP_PutALittleDirtUnderThePillow;

    public static readonly JobDef MSSFP_UseVent;
    public static readonly JobDef MSSFP_SusDeconstruct;

    public static readonly PawnKindDef MSSFP_Dirtman;

    public static readonly PawnKindDef MSSFP_MogusKind_Blue;
    public static readonly PawnKindDef MSSFP_MogusKind_Red;
    public static readonly PawnKindDef MSSFP_MogusKind_Green;
    public static readonly PawnKindDef MSSFP_MogusKind_Yellow;

    public static readonly SoundDef MSSFP_EmergencyMeetingKlaxon;
    public static readonly AbilityDef MSSFP_EmergencyMeeting;

    public static readonly WorkTypeDef MSS_AnomalyPrevention;

    public static readonly ThingDef MSS_PawnFlyer_Balloon;
    public static readonly ThingDef MSS_Balloon;

    public static readonly JobDef MSSFP_ExtractTarget;

    public static readonly ThingDef MSS_Drone;

    public static readonly SoundDef MSS_DroneFly;
    public static readonly SoundDef MSS_DroneExplode;

    static MSSFPDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MSSFPDefOf));
}
