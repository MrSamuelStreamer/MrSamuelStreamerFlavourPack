using System.Xml;
using Verse;

namespace MSSFP.Utils;

public class HediffChance
{
    public HediffDef hediff;
    public float chance;

    public HediffChance() { }

    public HediffChance(HediffDef hediff, float chance)
    {
        this.hediff = hediff;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "hediff", xmlNode.Name);
        chance = ParseHelper.FromString<float>(xmlNode.FirstChild.Value);
    }
}
