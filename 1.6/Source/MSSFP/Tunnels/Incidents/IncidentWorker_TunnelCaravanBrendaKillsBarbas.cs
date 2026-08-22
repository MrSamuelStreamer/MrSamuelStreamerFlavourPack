using System.Collections.Generic;
using MSSFP.Tunnels.Lords;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: a mysterious female figure (Brenda) and her dog (Barbas) block the tunnel.
/// Brenda immediately executes Barbas, then turns hostile to the caravan.
///
/// Brenda belongs to the AncientsHostile faction (permanent enemy, no goodwill impact).
/// She is a Sanguophage when Biotech is active, otherwise a SpaceRefugee.
/// Barbas is a Labrador Retriever spawned near the map centre (not at the edge with Brenda).
///
/// Fires at most once per save game — tracked on <see cref="TunnelGenData.brendaFired"/>
/// rather than a generic per-defName GameComponent, so the flag survives a mod
/// uninstall/reinstall as part of the save's own world-component state.
/// </summary>
public class IncidentWorker_TunnelCaravanBrendaKillsBarbas : IncidentWorker_TunnelCaravanSomethingHappened
{
    private Pawn _brenda;
    private Pawn _barbas;

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;

        TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
        if (comp != null && comp.brendaFired) return false;

        return true;
    }

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        // Brenda belongs to the hostile ancients — a permanent enemy faction that
        // has no allies, so she won't trigger goodwill penalties against anyone.
        parms.faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.AncientsHostile);

        PawnKindDef kind = ModsConfig.BiotechActive && PawnKindDefOf.Sanguophage != null
            ? PawnKindDefOf.Sanguophage
            : PawnKindDefOf.SpaceRefugee;

        Pawn brenda = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            kind,
            faction: parms.faction,
            context: PawnGenerationContext.NonPlayer,
            fixedGender: Gender.Female,
            forceGenerateNewPawn: true,
            allowDead: false,
            allowDowned: false,
            canGeneratePawnRelations: false,
            mustBeCapableOfViolence: true
        ));

        brenda.Name = new NameTriple("Brenda", "Brenda", "");
        _brenda = brenda;
        return new List<Pawn> { brenda };
    }

    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        // At this point SetupCaravanAttackMap has already spawned Brenda at a map-edge cell.
        Map map = _brenda?.MapHeld;
        if (map == null)
            return;

        Pawn barbas = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDef.Named("LabradorRetriever"),
            faction: null,
            context: PawnGenerationContext.NonPlayer,
            forceGenerateNewPawn: true,
            allowDead: false,
            allowDowned: false,
            canGeneratePawnRelations: false,
            mustBeCapableOfViolence: false
        ));
        barbas.Name = new NameSingle("Barbas");

        // Spawn Barbas near the map centre so Brenda has to walk to him.
        GenSpawn.Spawn(barbas, FindCellNearCenter(map), map, Rot4.Random);
        _barbas = barbas;
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
    {
        return new LordJob_BrendaKillsBarbas(_barbas);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        bool result = base.TryExecuteWorker(parms);

        if (result)
        {
            TunnelGenData comp = Find.World?.GetComponent<TunnelGenData>();
            if (comp != null)
                comp.brendaFired = true;
        }

        return result;
    }
}
