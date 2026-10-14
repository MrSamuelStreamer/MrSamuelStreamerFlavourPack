using HarmonyLib;
using Verse;

namespace MSSFP.IS;

public class MSSFPISMod : Mod
{
    public MSSFPISMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPISMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.IS.main");
        harmony.PatchAll();
    }
}
