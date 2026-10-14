using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Combat incident — gated on MSSFPMod.settings.AllowCombatTunnelIncidents (in addition
/// to the standard tunnel-system enable/disable + caravan guards) since this class cannot
/// inherit from IncidentWorker_TunnelCaravanSomethingHappened.
/// </summary>
public class IncidentWorker_TunnelCaravanFactionAmbush : IncidentWorker_Ambush_EnemyFaction
{
    /// <summary>
    /// Captures the TunnelCaravan's travel state into TunnelEncounterSetup statics before
    /// calling base.TryExecuteWorker. The caravan is destroyed by CaravanEnterMapUtility.Enter
    /// during base execution, so the capture must happen first.
    /// Cannot inherit from IncidentWorker_TunnelCaravanSomethingHappened because this class
    /// needs IncidentWorker_Ambush_EnemyFaction as its base for goodwill and faction naming.
    /// </summary>
    protected override void PostProcessGeneratedPawnsAfterSpawning(List<Pawn> generatedPawns)
    {
        base.PostProcessGeneratedPawnsAfterSpawning(generatedPawns);
        Map map = generatedPawns.Count > 0 ? generatedPawns[0].Map : null;
        if (map != null)
            TunnelUtilities.TrySpawnRubyVeins(map);
    }

    protected override bool TryExecuteWorker(IncidentParms parms)
    {
        IncidentWorker_TunnelCaravanSomethingHappened.CaptureEncounterSetup(parms);
        return base.TryExecuteWorker(parms);
    }

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        if (!TunnelUtilities.IsEnabled()) return false;
        if (MSSFPMod.settings.DisableTunnelIncidents) return false;
        if (!MSSFPMod.settings.AllowCombatTunnelIncidents) return false;

        if (parms.target is not TunnelCaravan tunnelCaravan)
            return false;

        if (!IncidentWorker_TunnelCaravanSomethingHappened.MeetsCaravanGuard(tunnelCaravan))
            return false;

        return base.CanFireNowSub(parms);
    }

    protected override string GetLetterText(Pawn anyPawn, IncidentParms parms)
    {
        Caravan caravan = parms.target as Caravan;
        string caravanName = caravan != null ? caravan.Name : "yourCaravan".TranslateSimple();
        return def.letterText
            .Formatted(caravanName, parms.faction.def.pawnsPlural, parms.faction.NameColored)
            .Resolve()
            .CapitalizeFirst();
    }
}
