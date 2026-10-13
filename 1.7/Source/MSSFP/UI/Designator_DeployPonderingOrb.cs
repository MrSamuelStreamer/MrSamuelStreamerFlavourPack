using MSSFP.Comps;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Dialogs;

/// <summary>
/// Designator placed when the player clicks a loaded persona core's "Deploy Pondering Orb"
/// gizmo. Subclass of <see cref="Designator_Build"/> with the placing-def hard-set to
/// <c>MSSFP_AICore_LoadedVariant</c>.
///
/// The variant building's costList wants <c>MSSFP_LoadedAIPersonaCore</c> instead of
/// <c>AIPersonaCore</c>, so vanilla <see cref="Frame.Accepts"/> + the construction WorkGiver
/// auto-haul the loaded core to the Frame without any custom hauling logic.
///
/// On Frame completion the <see cref="MSSFP.Buildings.Frame_AICoreLoaded"/> subclass extracts
/// the Pawn from the loaded core's <see cref="CompLoadedAIPersonaCore.storedHolo"/> and
/// transfers it into the new orb's projector — preserving identity across the destroy →
/// loaded core → redeploy cycle.
/// </summary>
public class Designator_DeployPonderingOrb : Designator_Build
{
    public Designator_DeployPonderingOrb(CompLoadedAIPersonaCore comp)
        : base(DefDatabase<ThingDef>.GetNamed("MSSFP_AICore_LoadedVariant"))
    {
        defaultLabel = "MSSFP_LoadedCore_Deploy".Translate();
        defaultDesc = "MSSFP_LoadedCore_DeployDesc".Translate();
        icon = ContentFinder<Texture2D>.Get("Things/Building/MSSFP_AI_Core_Icon", false)
            ?? BaseContent.BadTex;
    }
}
