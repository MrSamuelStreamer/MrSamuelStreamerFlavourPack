using System.Collections.Generic;
using HarmonyLib;
using MSSFP.PawnPortability.Export;
using MSSFP.PawnPortability.Settings;
using UnityEngine;
using Verse;

namespace MSSFP.HarmonyPatches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class Pawn_ExportGizmo_Patch
    {
        private static readonly Texture2D ExportIcon =
            ContentFinder<Texture2D>.Get("UI/MSSFP_ExportPawn");

        [HarmonyPostfix]
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (Gizmo gizmo in gizmos)
                yield return gizmo;

            if (!PawnPortabilitySettings.ExportGizmoEnabled)
                yield break;

            if (!__instance.IsColonistPlayerControlled && !__instance.IsPrisonerOfColony)
                yield break;

            yield return CreateExportGizmo(__instance);
        }

        private static Command_Action CreateExportGizmo(Pawn pawn)
        {
            return new Command_Action
            {
                defaultLabel = "Export Pawn",
                defaultDesc = "Export this pawn as a PawnTemplateDef XML file.",
                icon = ExportIcon,
                action = () => Find.WindowStack.Add(new Dialog_ExportPawnTemplate(pawn))
            };
        }
    }
}
