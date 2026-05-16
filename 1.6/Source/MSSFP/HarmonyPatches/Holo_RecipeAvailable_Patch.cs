using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MSSFP.Holo;
using Verse;

namespace MSSFP.HarmonyPatches
{
    /// <summary>
    /// Postfix on every <see cref="RecipeWorker.AvailableOnNow(Thing, BodyPartRecord)"/>
    /// override in the assembly tree. A single patch on the base method only fires for callers
    /// who reach the base via <c>base.AvailableOnNow</c> — vanilla <c>Recipe_Surgery</c> and
    /// many subclasses fully override without chaining, so virtual dispatch skips the base
    /// MethodInfo. <see cref="TargetMethods"/> enumerates all overrides explicitly.
    ///
    /// Holos are illusory bodies — no surgery, no implant install/removal, no organ harvest.
    /// Tend (JobDriver_TendPatient, not a Recipe) is unaffected.
    /// </summary>
    [HarmonyPatch]
    public static class Holo_RecipeAvailable_Patch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type baseType = typeof(RecipeWorker);
            ParameterModifier[] empty = Array.Empty<ParameterModifier>();
            Type[] sig = { typeof(Thing), typeof(BodyPartRecord) };

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }

                foreach (Type t in types)
                {
                    if (t == null) continue;
                    if (!baseType.IsAssignableFrom(t)) continue;
                    if (t.IsAbstract) continue;
                    MethodInfo mi = t.GetMethod(
                        nameof(RecipeWorker.AvailableOnNow),
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                        binder: null,
                        types: sig,
                        modifiers: empty);
                    if (mi != null) yield return mi;
                }
            }
        }

        public static void Postfix(ref bool __result, Thing thing)
        {
            if (!__result) return;
            if (thing is Pawn p && MSSFPHoloUtil.IsHolo(p))
                __result = false;
        }
    }
}
