using HarmonyLib;
using Verse;

namespace MSSFP.VOE;

public class MSSFPVOEMod : Mod
{
    public MSSFPVOEMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVOEMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VOE.main");
        harmony.PatchAll();
    }
}
