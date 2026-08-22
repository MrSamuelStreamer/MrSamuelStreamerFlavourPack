using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Tunnel encounter: a swarm of insects blocks the passage.
/// Extends IncidentWorker_TunnelCaravanSomethingHappened so the TunnelEncounterSetup
/// capture and CanFireNowSub guard are handled by the base class.
/// Combat incident — gated additionally on MSSFPMod.settings.AllowCombatTunnelIncidents.
/// </summary>
public class IncidentWorker_TunnelCaravanInsectAttack : IncidentWorker_TunnelCaravanSomethingHappened
{
    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!MSSFPMod.settings.AllowCombatTunnelIncidents) return false;
        return base.CanFireNowSub(parms);
    }

    protected override LordJob CreateLordJob(List<Pawn> generatedPawns, IncidentParms parms)
    {
        return new LordJob_AssaultColony(parms.faction, canTimeoutOrFlee: false);
    }

    protected override List<Pawn> GeneratePawns(IncidentParms parms)
    {
        parms.faction = Faction.OfInsects;

        List<Pawn> insects = new List<Pawn>();

        float points = parms.points > 0f ? parms.points : 300f;

        PawnKindDef[] insectKinds = new[]
        {
            PawnKindDefOf.Megaspider,
            PawnKindDefOf.Spelopede,
            PawnKindDefOf.Megascarab
        };

        while (points > 0f && insects.Count < 50)
        {
            PawnKindDef insectKind = insectKinds[Rand.Range(0, insectKinds.Length)];
            float cost = insectKind.combatPower;

            if (cost > points && insects.Count > 0)
                break;

            Pawn insect = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                insectKind,
                Faction.OfInsects,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: false,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: true
            ));

            insects.Add(insect);
            points -= cost;
        }

        return insects;
    }
}
