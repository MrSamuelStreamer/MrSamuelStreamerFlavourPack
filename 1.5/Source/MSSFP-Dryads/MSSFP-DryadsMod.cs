using Verse;
using HarmonyLib;

namespace MSSFP.Dryads;

public class MSSFPDryadsMod : Mod
{

    public MSSFPDryadsMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from MSSFPDryadsMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.Dryads.main");
        harmony.PatchAll();
    }
}
