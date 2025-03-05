using Verse;
using HarmonyLib;
using RimWorld;

namespace MSSFP.VET;

public class MSSFPVETMod : Mod
{
    public MSSFPVETMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVETMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VET.main");
        harmony.PatchAll();
    }
}
