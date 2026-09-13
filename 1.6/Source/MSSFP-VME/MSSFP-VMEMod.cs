using HarmonyLib;
using Verse;

namespace MSSFP.VME;

public class MSSFPVMEMod : Mod
{
    public MSSFPVMEMod(ModContentPack content)
        : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVMEMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VME.main");
        harmony.PatchAll();
    }
}

[StaticConstructorOnStartup]
internal static class MSSFPVMEStartup
{
    static MSSFPVMEStartup()
    {
        BloodCourtTooltipUtil.Apply();
    }
}
