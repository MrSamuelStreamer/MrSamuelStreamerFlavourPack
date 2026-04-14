using System;
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

        if (!PawnPortability.PawnPortability.AllDefsIncludingUser.Any(d => !PawnPortability.PawnPortability.IsAlive(d)))
            return false;

        return CanSpawnJoiner(map);
    }

    public override Pawn GeneratePawn()
    {
        var available = PawnPortability.PawnPortability.AllDefsIncludingUser
            .Where(d => !PawnPortability.PawnPortability.IsAlive(d))
            .ToList();

        if (!available.TryRandomElement(out PawnTemplateDef template))
            return null;

        return PawnPortability.PawnPortability.Create(template.defName, Faction.OfPlayer);
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
            ModLog.Warn("[WandererJoin_Template] GeneratePawn returned null — no available templates");
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
