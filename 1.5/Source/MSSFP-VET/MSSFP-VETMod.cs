using Verse;
using HarmonyLib;
using MSSFP.VFE;

namespace MSSFP.VET;

public class MSSFPVETMod : Mod
{

    public MSSFPVETMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVETMod");

        VFETribals.Utils.advancementPrecepts.Add(MSSFPVETDefOf.MSSFP_AdvanceToArcho);

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VET.main");
        harmony.PatchAll();
    }
}
