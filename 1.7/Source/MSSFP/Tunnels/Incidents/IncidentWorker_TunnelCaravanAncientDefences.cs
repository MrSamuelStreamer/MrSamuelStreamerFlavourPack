using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: dormant mechanoid sentinels activate and attack the tunnel caravan.
/// No custom lord job — vanilla LordJob_AssaultColony handles the assault.
/// Mechanoids are spawned via PawnGroupMakerUtility so the encounter uses whatever
/// mechanoid types the faction actually has defined, rather than hardcoded def names.
///
/// FireOncePerGame is false — mechanoid patrols are a recurring hazard of ancient
/// tunnel infrastructure, not a unique narrative event.
///
/// Not gated by MSSFPMod.settings.AllowCombatTunnelIncidents — that setting scopes
/// only the two dedicated raid-style combat incidents (InsectAttack, FactionAmbush);
/// AncientDefences is a smaller, recurring hazard event (category ThreatSmall).
/// </summary>
public class IncidentWorker_TunnelCaravanAncientDefences : IncidentWorker_TunnelCaravanSomethingHappened
{
    // Raid points budget: enough for a modest mechanoid patrol (2–4 Scythers/Centipedes).
    private static readonly FloatRange RaidPoints = new FloatRange(300f, 600f);

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!base.CanFireNowSub(parms)) return false;
        if (Faction.OfMechanoids == null) return false;
        if (Faction.OfMechanoids.def.pawnGroupMakers.NullOrEmpty()) return false;
        return true;
    }

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        parms.faction = Faction.OfMechanoids;

        return PawnGroupMakerUtility.GeneratePawns(new PawnGroupMakerParms
        {
            groupKind = PawnGroupKindDefOf.Combat,
            faction = Faction.OfMechanoids,
            points = RaidPoints.RandomInRange,
        }, warnOnZeroResults: true).ToList();
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
        => new LordJob_AssaultColony(
            Faction.OfMechanoids,
            canKidnap: false,
            canTimeoutOrFlee: false,
            sappers: false,
            useAvoidGridSmart: false,
            canSteal: false,
            breachers: false);
}
