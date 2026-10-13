using System.Collections.Generic;
using MSSFP.Tunnels.MapComponents;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MSSFP.Tunnels.Incidents;

/// <summary>
/// Incident: while the caravan rests in the tunnel, one traveller hears a distant voice
/// shouting "Can I get a hoo-ya in tunnel system?" followed by booming laughter.
///
/// No combat, no items, no NPCs. The caravan can reform immediately.
///
/// DoNonCombatExecute is overridden (instead of PostSetupEncounterMap) because the
/// pawn name must be resolved before SendStandardLetter is called, and the base sends
/// def.letterText statically inside DoNonCombatExecute after PostSetupEncounterMap
/// returns — so we build a dynamic string here and send it directly.
/// </summary>
public class IncidentWorker_TunnelCaravanVoiceInTheDark : IncidentWorker_TunnelCaravanNonCombat
{
    protected override void DoNonCombatExecute(IncidentParms parms)
    {
        Map map = CaravanIncidentUtility.SetupCaravanAttackMap(
            (Caravan)parms.target,
            new List<Pawn>(),
            sendLetterIfRelatedPawns: false);

        // No hostiles — CheckWonBattle fires immediately without this guard.
        map.components.Add(new MapComponent_SuppressBattleWon(map));

        // Pick a random colonist who heard the voice. FreeColonistsSpawned is
        // populated by CaravanEnterMapUtility.Enter as part of SetupCaravanAttackMap,
        // so all player pawns are already on the map at this point.
        string pawnLabel = map.mapPawns.FreeColonistsSpawned
            .RandomElementWithFallback()?.LabelShort ?? "a traveller";

        TaggedString text = "MSSFP_Tunnels_Incidents_VoiceInTheDark_Letter"
            .Translate(pawnLabel.Named("PAWN"));

        SendStandardLetter(def.letterLabel, text, def.letterDef, parms, map.Parent);
    }
}
