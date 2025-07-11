using System.Text;
using BigAndSmall;
using UnityEngine;
using Verse;

namespace MSSFP.BS;

public class Hediff_Bigger : HediffWithComps
{
    public override string Label => base.Label.Replace("MSS_Bigger_Label", "MSS_Bigger_Label".Translate(Mathf.Floor(Severity).ToStringPercent()));

    public override bool Visible => false;

    public override string Description
    {
        get
        {
            StringBuilder stringBuilder = new(def.Description.Translate(Mathf.Floor(Severity).ToStringPercent()));
            int index = 0;
            while (true)
            {
                int num = index;
                int? count = comps?.Count;
                int valueOrDefault = count.GetValueOrDefault();
                if ((num < valueOrDefault) & count.HasValue)
                {
                    string descriptionExtra = comps[index].CompDescriptionExtra;
                    if (!descriptionExtra.NullOrEmpty())
                    {
                        stringBuilder.Append(" ");
                        stringBuilder.Append(descriptionExtra);
                    }

                    ++index;
                }
                else
                {
                    break;
                }
            }

            return stringBuilder.ToString();
        }
    }

    public float BodySizeMultiplier = 1f;
    public bool SizeChanged = false;
    public bool RecalculateSize = false;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref BodySizeMultiplier, "BodySizeMultiplier", 1f);
    }

    public override void PostTick()
    {
        base.PostTick();
        if (RecalculateSize)
        {
            RecalculateSize = false;
            HumanoidPawnScaler.GetCache(pawn, forceRefresh: true, scheduleForce: 10);
        }
        if (SizeChanged)
        {
            SizeChanged = false;
            RecalculateSize = true;
        }
    }
}
