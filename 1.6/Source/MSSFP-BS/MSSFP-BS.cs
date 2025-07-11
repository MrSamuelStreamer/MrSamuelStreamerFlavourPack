using HarmonyLib;
using Verse;

namespace MSSFP.BS;

public class MSSFPBSMod : Mod
{
    public MSSFPBSMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPBSMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.BS.main");
        harmony.PatchAll();
    }
}
