using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MSSFP.PawnPortability.Defs;
using MSSFP.PawnPortability.Settings;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Portability = MSSFP.PawnPortability.PawnPortability;

namespace MSSFP.HarmonyPatches;

/// <summary>
/// With a configurable chance, replaces an eligible non-player pawn generation
/// (raiders, traders, visitors, faction pawns) with a pawn built from a
/// PawnTemplateDef. Each template is used at most once per game.
/// </summary>
[HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), typeof(PawnGenerationRequest))]
public static class PawnGenerator_TemplateInjection_Patch
{
    // PawnTemplateSpawner.Create calls GeneratePawn for its base pawn; without
    // this guard that inner call could itself be replaced by another template.
    [ThreadStatic]
    private static bool creatingTemplate;

    [HarmonyPrefix]
    public static bool Prefix(PawnGenerationRequest request, ref Pawn __result)
    {
        if (creatingTemplate
            || !PawnPortabilitySettings.TemplateInjectionEnabled
            || Current.ProgramState != ProgramState.Playing
            || !IsRequestEligible(request)
            || !Rand.Chance(PawnPortabilitySettings.TemplateInjectionChance))
            return true;

        Pawn pawn = TryCreateFromTemplate(request);
        if (pawn == null) return true;

        __result = pawn;
        return false;
    }

    private static Pawn TryCreateFromTemplate(PawnGenerationRequest request)
    {
        List<PawnTemplateDef> pool = Portability.AvailableForRandomGeneration.ToList();
        if (!pool.TryRandomElement(out PawnTemplateDef def)) return null;

        Pawn pawn;
        creatingTemplate = true;
        try
        {
            pawn = Portability.Create(def, request.Faction, request.KindDef);
        }
        finally
        {
            creatingTemplate = false;
        }

        if (pawn == null) return null;

        if (!PassesCallerChecks(request, pawn))
        {
            if (PawnPortabilitySettings.LoggingEnabled)
                ModLog.Log($"[PawnPortability] Injection skipped {def.defName}: failed the caller's pawn checks");
            Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
            return null;
        }

        Portability.RegisterRandomGeneration(def, pawn);
        if (PawnPortabilitySettings.LoggingEnabled)
            ModLog.Log($"[PawnPortability] Injected template {def.defName} as {request.KindDef?.defName} for {request.Faction?.Name}");
        return pawn;
    }

    /// <summary>
    /// Only unconstrained requests may be replaced. Callers that fix a pawn's
    /// identity, genetics or age (relation generation, quests, creep joiners)
    /// depend on those fields; a template pawn would silently ignore them.
    /// </summary>
    internal static bool IsRequestEligible(PawnGenerationRequest request) =>
        IsKindAndFactionEligible(request)
        && !HasIdentityConstraints(request)
        && !HasBiologyConstraints(request);

    private static bool IsKindAndFactionEligible(PawnGenerationRequest request)
    {
        PawnKindDef kind = request.KindDef;
        if (kind?.RaceProps?.Humanlike != true || kind.factionLeader) return false;

        // Create() maps a null faction to the player, so factionless requests are out too.
        Faction faction = request.Faction;
        if (faction == null || faction.IsPlayer) return false;

        return request.Context != PawnGenerationContext.PlayerStarter
               && (request.AllowedDevelopmentalStages & DevelopmentalStage.Adult) != 0
               && !request.IsCreepJoiner
               && !request.ForceDead
               && !request.ForceNoGear;
    }

    // Raid groups pass the faction's own ideo; only a different ideo is a real constraint.
    private static bool HasIdentityConstraints(PawnGenerationRequest request) =>
        request.FixedGender.HasValue
        || !string.IsNullOrEmpty(request.FixedLastName)
        || !string.IsNullOrEmpty(request.FixedBirthName)
        || request.FixedTitle != null
        || (request.FixedIdeo != null && request.FixedIdeo != request.Faction.ideos?.PrimaryIdeo)
        || request.OnlyUseForcedBackstories
        || !request.ForcedTraits.EnumerableNullOrEmpty();

    private static bool HasBiologyConstraints(PawnGenerationRequest request) =>
        request.FixedBiologicalAge.HasValue
        || request.FixedChronologicalAge.HasValue
        || request.BiologicalAgeRange.HasValue
        || (request.ForcedXenotype != null && !IsNaturalXenotype(request))
        || request.ForcedCustomXenotype != null
        || request.ForcedMutant != null
        || !request.ForcedXenogenes.NullOrEmpty()
        || !request.ForcedEndogenes.NullOrEmpty();

    /// <summary>
    /// Group generation (raids, traders) pre-rolls a xenotype from the kind/faction
    /// xenotype set and passes it as ForcedXenotype. That is a random pick, not a
    /// caller requirement, so it doesn't block injection.
    /// </summary>
    private static bool IsNaturalXenotype(PawnGenerationRequest request) =>
        PawnGenerator.XenotypesAvailableFor(request.KindDef, request.Faction.def, request.Faction)
            .ContainsKey(request.ForcedXenotype);

    /// <summary>
    /// Post-creation checks the caller relies on: raid strategies validate pawns
    /// (e.g. sappers, breachers), and raid/defence callers need fighters.
    /// </summary>
    private static bool PassesCallerChecks(PawnGenerationRequest request, Pawn pawn)
    {
        if (request.MustBeCapableOfViolence && pawn.WorkTagIsDisabled(WorkTags.Violent)) return false;
        if (request.ValidatorPreGear != null && !request.ValidatorPreGear(pawn)) return false;
        return request.ValidatorPostGear == null || request.ValidatorPostGear(pawn);
    }
}
