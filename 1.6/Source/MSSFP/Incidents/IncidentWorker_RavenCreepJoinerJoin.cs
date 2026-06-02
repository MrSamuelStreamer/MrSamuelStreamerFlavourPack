using System;
using System.Linq;
using System.Reflection;
using MSSFP.Compatibility.BigAndSmall;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_RavenCreepJoinerJoin : IncidentWorker_WandererJoin
{
    private static MethodInfo _swapMethod;
    private static bool _swapMethodResolved;

    private static MethodInfo SwapMethod
    {
        get
        {
            if (_swapMethodResolved) return _swapMethod;
            _swapMethodResolved = true;
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "BigAndSmall");
            _swapMethod = asm?.GetType("BigAndSmall.RaceMorpher")
                ?.GetMethod("SwapAnimalToSapientVersion",
                    BindingFlags.Public | BindingFlags.Static);
            return _swapMethod;
        }
    }

    public override float ChanceFactorNow(IIncidentTarget target) =>
        base.ChanceFactorNow(target) * MSSFPMod.settings.RavenCreepJoinerChanceMultiplier;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!MSSFPMod.settings.EnableRavenCreepJoiner)
            return false;

        if (parms.target is not Map map)
            return false;

        if (DefDatabase<PawnKindDef>.GetNamedSilentFail("MSSFP_Raven") == null)
            return false;

        return CanSpawnJoiner(map);
    }

    public override Pawn GeneratePawn()
    {
        var kind = DefDatabase<PawnKindDef>.GetNamedSilentFail("MSSFP_Raven");
        if (kind == null)
        {
            ModLog.Warn("[RavenCreepJoiner] MSSFP_Raven PawnKindDef missing");
            return null;
        }

        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            kind,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            fixedBiologicalAge: 2f,
            fixedChronologicalAge: 2f
        ));

        if (SwapMethod != null)
        {
            try
            {
                var result = SwapMethod.Invoke(null, new object[] { pawn });
                if (result is Pawn swapped)
                    pawn = swapped;
            }
            catch (Exception e)
            {
                ModLog.Warn($"[RavenCreepJoiner] SwapAnimalToSapientVersion threw: {e.Message}");
            }
        }
        else
        {
            ModLog.Warn("[RavenCreepJoiner] BigAndSmall.RaceMorpher.SwapAnimalToSapientVersion missing — Big & Small framework not loaded");
        }

        // B&S's SwapAnimalToSapientVersion leaves a subset of humanlike trackers null
        // (relations, skills, workSettings, playerSettings, ideo, timetable, drugs,
        // outfits, foodRestriction, records, royalty). RimWorld and many downstream mods
        // assume those exist on a humanlike pawn and NRE on access — see
        // SapientRaven_TrackerRepair for the full cascade. Repair before continuing.
        SapientRaven_TrackerRepair.EnsureHumanlikeTrackers(pawn);

        var auraSource = DefDatabase<HediffDef>.GetNamedSilentFail("MSSFP_RavenSavantAuraSource");
        if (auraSource != null && pawn?.health != null && !pawn.health.hediffSet.HasHediff(auraSource))
            pawn.health.AddHediff(auraSource);

        return pawn;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Map map)
            return false;

        if (!CanSpawnJoiner(map))
            return false;

        Pawn pawn = GeneratePawn();
        if (pawn == null)
        {
            ModLog.Warn("[RavenCreepJoiner] GeneratePawn returned null");
            return false;
        }

        SpawnJoiner(map, pawn);

        TaggedString text = def.letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
        TaggedString label = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
        PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
        SendStandardLetter(label, text, LetterDefOf.PositiveEvent, parms, pawn, Array.Empty<NamedArgument>());
        return true;
    }
}
