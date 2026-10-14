using System.Linq;
using MSSFP.Comps;
using MSSFP.Gravship;
using RimWorld;
using RimWorld.Planet;
using VanillaGravshipExpanded;
using Verse;

namespace MSSFP.VGE;

/// <summary>
/// Vanilla Gravship Expanded-flavoured orb jump-events. This assembly is built into
/// Compatibility/vanillaexpanded.gravship/ and only loads when VGE is active (see loadFolders.xml),
/// so these events auto-register into <see cref="OrbJumpEventManager"/>'s reflection pool exactly
/// when VGE is present — and are simply absent otherwise.
/// </summary>
internal static class VGEOrbUtil
{
    public static Building_GravshipBlackBox FindBlackBox(Building_GravEngine engine)
    {
        return engine
            .GravshipComponents.Select(c => c.parent)
            .OfType<Building_GravshipBlackBox>()
            .FirstOrDefault();
    }
}

/// <summary>The AI corrupted the trip's gravdata. The black box is wiped.</summary>
public class OrbEvent_GravdataCorruption : OrbJumpEvent
{
    public override string Label => "VGE_GravdataCorruption";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Bad;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        Building_GravshipBlackBox box = VGEOrbUtil.FindBlackBox(engine);
        if (box != null && box.StoredGravdata > 0f)
            box.TakeGravdata(box.StoredGravdata);

        SendLetter(
            "MSSFP_OrbEvent_VGE_GravdataCorruption_Label",
            "MSSFP_OrbEvent_VGE_GravdataCorruption_Text",
            LetterDefOf.NegativeEvent,
            engine,
            orb
        );
    }
}

/// <summary>A gravtech windfall: the persona squeezed extra gravdata out of the route.</summary>
public class OrbEvent_GravtechWindfall : OrbJumpEvent
{
    public override string Label => "VGE_GravtechWindfall";
    public override OrbEventFlavour Flavour => OrbEventFlavour.Good;

    public override void Fire(RimWorld.Planet.Gravship gravship, Building_GravEngine engine, CompTrueAICore orb)
    {
        Building_GravshipBlackBox box = VGEOrbUtil.FindBlackBox(engine);
        if (box != null)
            box.AddGravdata(Rand.Range(150f, 400f));

        SendMessage("MSSFP_OrbEvent_VGE_GravtechWindfall_Msg", MessageTypeDefOf.PositiveEvent, engine, orb);
    }
}
