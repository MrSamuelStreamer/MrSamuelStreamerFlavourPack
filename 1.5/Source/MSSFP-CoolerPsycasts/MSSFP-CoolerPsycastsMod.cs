using Verse;
using HarmonyLib;

namespace MSSFP.CoolerPsycasts;

public class MSSFPCoolerPsycastsMod : Mod
{

    public MSSFPCoolerPsycastsMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from MSSFPCoolerPsycasts");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.CoolerPsycasts.main");
        harmony.PatchAll();
    }
}
