using HarmonyLib;
using Verse;

namespace MSSFP.GeneratorMod;

public class MSSFPGeneratorMod : Mod
{
    public static GeneratorSettingsTab settings => MSSFPMod.settings.GetSettings<GeneratorSettingsTab>();

    public MSSFPGeneratorMod(ModContentPack content)
        : base(content)
    {
#if DEBUG
        Harmony.DEBUG = true;
#endif
        if (settings.EnableFasterUpgrades)
        {
            Harmony harmony = new Harmony("MrSamuelStreamer.rimworld.MSSFP.Generator.main");
            harmony.PatchAll();
        }
    }
}
