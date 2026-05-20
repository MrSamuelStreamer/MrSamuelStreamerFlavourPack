using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using LoanMod;
using Verse;

namespace MSSFP.DBG
{
    /// <summary>
    /// Transpiler that raises the two hardcoded clamp ceilings inside
    /// <see cref="ScenPart_Loan.DoEditInterface"/>:
    /// <list type="bullet">
    ///   <item><c>initialLoanAmount</c> max: 1,000,000 -&gt; 100,000,000 (100x)</item>
    ///   <item><c>annualPaymentAmount</c> max: 100,000 -&gt; 50,000,000 (500x)</item>
    /// </list>
    /// Minimums (100, 10) are left untouched.
    ///
    /// Matches by exact Ldc_R4 operand rather than ordinal position so a future
    /// UI row reorder will not break the patch. The values 1,000,000f and
    /// 100,000f are unique within the current <c>DoEditInterface</c> body
    /// (verified against the source shipped with Workshop id 3529144158); if
    /// RER later reuses either constant elsewhere in the same method, the
    /// substitution would over-apply. The post-pass hit-count check logs a
    /// warning if the expected number of substitutions does not match.
    /// </summary>
    [HarmonyPatch(typeof(ScenPart_Loan), nameof(ScenPart_Loan.DoEditInterface))]
    public static class ScenPartLoan_RaiseCaps_Patch
    {
        private const float OldPrincipalMax = 1_000_000f;
        private const float NewPrincipalMax = 100_000_000f;

        private const float OldAnnualMax = 100_000f;
        private const float NewAnnualMax = 50_000_000f;

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            int principalHits = 0;
            int annualHits = 0;

            foreach (CodeInstruction ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldc_R4 && ins.operand is float f)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (f == OldPrincipalMax)
                    {
                        ins.operand = NewPrincipalMax;
                        principalHits++;
                    }
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    else if (f == OldAnnualMax)
                    {
                        ins.operand = NewAnnualMax;
                        annualHits++;
                    }
                }
                yield return ins;
            }

            if (principalHits != 1 || annualHits != 1)
            {
                Log.Warning(
                    "[MSSFP.DBG] ScenPart_Loan.DoEditInterface transpiler expected exactly one "
                    + $"substitution per constant but saw principal={principalHits}, annual={annualHits}. "
                    + "RER may have refactored the editor; caps may be partially raised or unchanged."
                );
            }
        }
    }
}
