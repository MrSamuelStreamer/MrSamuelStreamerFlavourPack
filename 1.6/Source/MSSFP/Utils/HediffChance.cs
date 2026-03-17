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
        XmlAttribute mayRequire = xmlNode?.Attributes?["MayRequire"];
        XmlAttribute mayRequireAnyOf = xmlNode?.Attributes?["MayRequireAnyOf"];
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "hediff", xmlNode?.Name, mayRequire?.Value.ToLower(), mayRequireAnyOf?.Value.ToLower());
        chance = ParseHelper.FromString<float>(xmlNode?.FirstChild.Value?? "0");
    }
}
