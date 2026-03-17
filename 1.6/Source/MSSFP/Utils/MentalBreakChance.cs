using System.Xml;
using Verse;

namespace MSSFP.Utils;

public class MentalBreakChance
{
    public MentalBreakDef mentalBreak;
    public float chance;

    public MentalBreakChance() { }

    public MentalBreakChance(MentalBreakDef mentalBreak, float chance)
    {
        this.mentalBreak = mentalBreak;
        this.chance = chance;
    }

    public void LoadDataFromXmlCustom(XmlNode xmlNode)
    {
        XmlAttribute mayRequire = xmlNode?.Attributes?["MayRequire"];
        XmlAttribute mayRequireAnyOf = xmlNode?.Attributes?["MayRequireAnyOf"];
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "mentalBreak", xmlNode?.Name, mayRequire?.Value.ToLower(), mayRequireAnyOf?.Value.ToLower());
        chance = ParseHelper.FromString<float>(xmlNode?.FirstChild.Value?? "0");
    }
}
