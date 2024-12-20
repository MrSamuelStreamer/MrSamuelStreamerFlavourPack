using Verse;
using HarmonyLib;

namespace MSSFP.VAE;

public class MSSFPVAEMod : Mod
{

    public MSSFPVAEMod(ModContentPack content) : base(content)
    {
        ModLog.Debug("Hello world from MSSFPVAEMod");

#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.VAE.main");
        harmony.PatchAll();
    }
}
