using Verse;
using HarmonyLib;

namespace MSSFP.VFE;

public class MSSFPVFEMod : Mod
{

    public MSSFPVFEMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVFEMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VFE.main");
        harmony.PatchAll();
    }
}
