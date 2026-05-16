using System.Collections.Generic;
using System.Text;
using MSSFP.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace MSSFP.Comps;

/// <summary>
/// On-item comp that holds a recalled holo Pawn + the persona def that owned it. Lives on
/// <c>MSSFP_LoadedAIPersonaCore</c> items dropped by destroyed/deconstructed Pondering Orbs.
///
/// Lifecycle:
///   1. <see cref="MSSFP.Buildings.Building_AICore.Destroy"/> creates a loaded core item and
///      transfers the projector's stored Pawn + activePersonality into this comp.
///   2. Item sits on the map. <see cref="IThingHolder"/> registration keeps the Pawn alive
///      in the colony's pawn graph (Find.WorldPawns won't reclaim it; ChildHolders chain).
///   3. Player clicks the loaded core, sees the "Deploy Pondering Orb" gizmo, opens
///      <see cref="MSSFP.Dialogs.Designator_DeployPonderingOrb"/>.
///   4. Designator places a Blueprint for <c>MSSFP_AICore_LoadedVariant</c>. Colonists haul
///      the loaded core (matched by costList ThingDef equality) into the Frame's
///      resourceContainer.
///   5. On Frame completion the custom <see cref="MSSFP.Buildings.Frame_AICoreLoaded"/>
///      subclass extracts the Pawn from storedHolo into its own pawnInTransit container,
///      destroys this loaded core (Vanish), then transfers the Pawn into the new orb's
///      CompHoloProjector.stored.
/// </summary>
public class CompLoadedAIPersonaCore : ThingComp, IThingHolder
{
    /// <summary>
    /// The recalled holo pawn. ThingOwner so the Pawn participates in vanilla scribe + pawn
    /// graph traversal — same pattern as <see cref="MSSFP.Holo.CompHoloProjector.stored"/>.
    /// </summary>
    public ThingOwner<Pawn> storedHolo;

    /// <summary>Active persona that drove the source orb. Restored on redeploy.</summary>
    public AIPersonalityDef storedPersonality;

    public override void Initialize(CompProperties propsArg)
    {
        base.Initialize(propsArg);
        storedHolo ??= new ThingOwner<Pawn>(this, oneStackOnly: false);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref storedHolo, "storedHolo", this);
        Scribe_Defs.Look(ref storedPersonality, "storedPersonality");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && storedHolo == null)
        {
            storedHolo = new ThingOwner<Pawn>(this, oneStackOnly: false);
        }
    }

    public ThingOwner GetDirectlyHeldThings() => storedHolo;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>)GetDirectlyHeldThings());
    }

    public override string CompInspectStringExtra()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("MSSFP_LoadedCore_StoredPersona".Translate(
            storedPersonality?.LabelShortOrLabel ?? "(none)"
        ));
        Pawn p = storedHolo != null && storedHolo.Count > 0 ? storedHolo[0] : null;
        if (p != null)
        {
            sb.AppendLine();
            sb.Append("MSSFP_LoadedCore_StoredPawn".Translate(p.LabelShortCap));
        }
        return sb.ToString();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
            yield return g;

        // No faction gate — items on the ground default to null Faction. The deploy
        // designator validates target cell + player ownership of the map at use time;
        // hiding the gizmo on null-faction items would make the loaded core unusable
        // immediately after the orb is deconstructed.
        if (!parent.Spawned)
            yield break;

        yield return new Command_Action
        {
            defaultLabel = "MSSFP_LoadedCore_Deploy".Translate(),
            defaultDesc = "MSSFP_LoadedCore_DeployDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("Things/Building/MSSFP_AI_Core_Icon", false)
                ?? BaseContent.BadTex,
            action = () =>
            {
                Find.DesignatorManager.Select(
                    new MSSFP.Dialogs.Designator_DeployPonderingOrb(this)
                );
            },
        };
    }
}
