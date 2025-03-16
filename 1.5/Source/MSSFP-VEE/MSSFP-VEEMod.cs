using HarmonyLib;
using Verse;

namespace MSSFP.VEE;

public class MSSFPVEEMod : Mod
{
    public MSSFPVEEMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVEEMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VEE.main");
        harmony.PatchAll();
    }
}
