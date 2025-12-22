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
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "mentalBreak", xmlNode.Name);
        chance = ParseHelper.FromString<float>(xmlNode.FirstChild.Value);
    }
}
