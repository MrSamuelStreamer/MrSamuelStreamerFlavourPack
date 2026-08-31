using HarmonyLib;
using Verse;

namespace MSSFP.RB;

public class MSSFPRBMod : Mod
{
    public MSSFPRBMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Loading MSSFP Rimbody compatibility layer");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.RB.main");
        harmony.PatchAll();
    }
}
