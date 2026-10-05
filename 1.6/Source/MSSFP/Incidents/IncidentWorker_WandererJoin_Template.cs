using System;
using System.Collections.Generic;
using System.Linq;
using MSSFP.PawnPortability;
using MSSFP.PawnPortability.Defs;
using RimWorld;
using Verse;

namespace MSSFP.Incidents;

public class IncidentWorker_WandererJoin_Template : IncidentWorker_WandererJoin
{
    public override float ChanceFactorNow(IIncidentTarget target) =>
        base.ChanceFactorNow(target) * MSSFPMod.settings.TemplateWandererJoinChanceMultiplier;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!MSSFPMod.settings.EnableTemplateWandererJoin)
            return false;

        if (parms.target is not Map map)
            return false;

        return PawnPortability.PawnPortability.AvailableForRandomGeneration.Any() && CanSpawnJoiner(map);

    }

    public override Pawn GeneratePawn(Map map)
    {
        List<PawnTemplateDef> available = [.. PawnPortability.PawnPortability.AvailableForRandomGeneration];

        if (!available.TryRandomElement(out PawnTemplateDef template))
            return null;

        Pawn pawn = PawnPortability.PawnPortability.Create(template, Faction.OfPlayer);
        if (pawn != null)
            PawnPortability.PawnPortability.RegisterRandomGeneration(template, pawn);
        return pawn;
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        if (parms.target is not Map map)
            return false;

        if (!CanSpawnJoiner(map))
            return false;

        Pawn pawn = GeneratePawn(map);
        if (pawn == null)
        {
            ModLog.Warn("[WandererJoin_Template] GeneratePawn returned null — no available templates");
            return false;
        }

        SpawnJoiner(map, pawn);

        TaggedString text = def.letterText.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
        TaggedString label = def.letterLabel.Formatted(pawn.Named("PAWN")).AdjustedFor(pawn, "PAWN", true);
        PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text, ref label, pawn);
        SendStandardLetter(label, text, LetterDefOf.PositiveEvent, parms, pawn);
        return true;
    }
}
