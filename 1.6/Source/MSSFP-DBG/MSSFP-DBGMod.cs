using HarmonyLib;
using Verse;

namespace MSSFP.DBG
{
    /// <summary>
    /// Compat-mod entry point for "Debt-Bound Gravship" (RER.GravshipLoanScenario,
    /// Steam id 3529144158).
    ///
    /// Loaded only when LoadFolders activates Compatibility/RER.GravshipLoanScenario,
    /// so the typed Harmony attributes on patches in this assembly may take
    /// compile-time references on LoanMod types without guarding.
    /// </summary>
    public class MSSFP_DBG_Mod : Mod
    {
        public MSSFP_DBG_Mod(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("MSSFP.DBG.RaiseCaps");
            harmony.PatchAll();
        }
    }
}
