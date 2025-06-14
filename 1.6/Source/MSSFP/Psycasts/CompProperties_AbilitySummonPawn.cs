using System.Collections.Generic;
using RimWorld;
using Verse;

namespace MSSFP.Psycasts;

public class CompProperties_AbilitySummonPawn : CompProperties_AbilityEffect
{
    public int count = 1;
    public List<PawnKindDef> pawnKinds = [];
    public MentalStateDef mentalState = null;
    public string mentalStateReason = null;
    public bool allowDowned = false;
    public bool allowDead = false;
    public bool recruitable = false;
    public bool forceGenerateNewPawn = false;
    public float colonistRelationChanceFactor = 0;
    public List<TraitDef> forcedTraits = [];
    public Gender? fixedGender = null;
    public string fixedLastName = null;
    public string fixedBirthName = null;
    public ThoughtDef thoughtOnSummon = null;

    public PawnKindDef RandomPawnKind => pawnKinds.NullOrEmpty() ? PawnKindDefOf.SpaceRefugee : pawnKinds.RandomElement();

    public CompProperties_AbilitySummonPawn()
    {
        compClass = typeof(CompAbilitySummonPawn);
    }
}
