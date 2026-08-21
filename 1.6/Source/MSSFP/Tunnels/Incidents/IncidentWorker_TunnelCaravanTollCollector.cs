using System.Collections.Generic;
using System.Linq;
using MSSFP.Tunnels.Lords;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: a lone armed figure demands 150 silver to let the caravan pass.
/// The player chooses to pay or fight via Dialog_NodeTree.
///
/// The toll collector belongs to a random hostile humanlike faction so that
/// LordToil_AssaultColony can correctly target player pawns (factionless pawns
/// are not registered as hostile in attackTargetsCache, making assault a no-op).
///
/// MapComponent_SuppressBattleWon is added to prevent the "Caravan victorious!"
/// letter from firing on peaceful payment (the pawn exits rather than dying, but
/// the map still has no remaining hostile threats after they leave).
///
/// State graph (LordJob_TollCollector):
///   standoff (DefendPoint at spawn position)
///     → "MSSFP_TollPaid"    → ExitMap
///     → "MSSFP_TollRefused" → AssaultColony
/// </summary>
public class IncidentWorker_TunnelCaravanTollCollector : IncidentWorker_TunnelCaravanSomethingHappened
{
    private Pawn _tollCollector;
    private IntVec3 _standPos;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        // Need a hostile humanlike faction so the pawn can actually fight on refusal.
        return Find.FactionManager.AllFactionsListForReading.Any(f =>
            !f.IsPlayer
            && f.HostileTo(Faction.OfPlayer)
            && !f.Hidden
            && f.def.humanlikeFaction
            && !f.def.pawnGroupMakers.NullOrEmpty());
    }

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        if (!TryFindHostileFaction(out Faction faction))
            return new List<Pawn>();

        parms.faction = faction;

        _tollCollector = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDef.Named("Drifter"),
            faction: faction,
            context: PawnGenerationContext.NonPlayer,
            forceGenerateNewPawn: true,
            allowDead: false,
            allowDowned: false,
            canGeneratePawnRelations: false,
            mustBeCapableOfViolence: true
        ));

        return new List<Pawn> { _tollCollector };
    }

    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        Pawn pawn = generatedPawns.Count > 0 ? generatedPawns[0] : null;
        if (pawn?.MapHeld == null) return;

        Map map = pawn.MapHeld;

        // Capture stand position (where SetupCaravanAttackMap spawned the pawn).
        _standPos = pawn.Position;

        // Suppress "Caravan victorious!" — fires on payment too, since the pawn
        // exits peacefully and the map then has no hostile threats.
        map.components.Add(new MapComponent_SuppressBattleWon(map));

        // Dialog component fires on first player-visible tick.
        map.components.Add(new MapComponent_TollCollectorDialog(map, _tollCollector));
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        => new LordJob_TollCollector(_standPos);

    private static bool TryFindHostileFaction(out Faction faction)
    {
        return Find.FactionManager.AllFactionsListForReading
            .Where(f =>
                !f.IsPlayer
                && f.HostileTo(Faction.OfPlayer)
                && !f.Hidden
                && f.def.humanlikeFaction
                && !f.def.pawnGroupMakers.NullOrEmpty())
            .TryRandomElement(out faction);
    }
}
