using System.Linq;
using RimWorld;
using Verse;

namespace MSSFP;

public class Verb_CastAbilityWithCost: Verb_CastAbility
{
    public virtual DamageWorker.DamageResult DamageBrain()
    {
        BodyPartRecord brain = CasterPawn.RaceProps.body.AllParts.FirstOrDefault(p=>p.def.defName == "Brain");
        if (brain == null) return null;

        DamageInfo dinfo = new DamageInfo(
            DamageDefOf.Scratch,
            1f,
            1f,
            -1f,
            CasterPawn,
            brain,
            spawnFilth: true);
        return CasterPawn.TakeDamage(dinfo);
    }

    protected override bool TryCastShot()
    {
        if(!base.TryCastShot()) return false;

        SkillRecord skillRecord = CasterPawn.skills.skills.Where(s=>!s.PermanentlyDisabled || !s.TotallyDisabled).RandomElementWithFallback();
        if (skillRecord is null)
        {
            DamageBrain();
            return true;
        };

        float xpForCurrentLevel = SkillRecord.XpRequiredToLevelUpFrom(skillRecord.levelInt);

        if (skillRecord.xpSinceLastLevel >= (xpForCurrentLevel * 0.10f))
        {
            if(CasterPawn.Faction.IsPlayer) Messages.Message("MSSFP_LostBrainPower".Translate(CasterPawn.NameShortColored, skillRecord.def.LabelCap), CasterPawn, MessageTypeDefOf.NegativeEvent, true);
            skillRecord.xpSinceLastLevel-=(xpForCurrentLevel*0.10f);

        }else if (skillRecord.xpSinceLastLevel < (xpForCurrentLevel * 0.10f) && skillRecord.xpSinceLastLevel > (xpForCurrentLevel * 0.05f))
        {
            if(CasterPawn.Faction.IsPlayer) Messages.Message("MSSFP_LostBrainPower".Translate(CasterPawn.NameShortColored, skillRecord.def.LabelCap), CasterPawn, MessageTypeDefOf.NegativeEvent, true);
            skillRecord.xpSinceLastLevel = 0;
        }
        else if(skillRecord.levelInt > 1)
        {
            if(CasterPawn.Faction.IsPlayer) Messages.Message("MSSFP_LostBrainPower".Translate(CasterPawn.NameShortColored, skillRecord.def.LabelCap), CasterPawn, MessageTypeDefOf.NegativeEvent, true);
            float xpForLastLevel = SkillRecord.XpRequiredToLevelUpFrom(skillRecord.levelInt - 1);
            skillRecord.levelInt--;
            skillRecord.xpSinceLastLevel = xpForLastLevel * 0.9f;
        }
        else
        {
            if(CasterPawn.Faction.IsPlayer) Messages.Message("MSSFP_LostBrainPowerDamaged".Translate(CasterPawn.NameShortColored, skillRecord.def.LabelCap), CasterPawn, MessageTypeDefOf.NegativeEvent, true);
            DamageBrain();
        }

        return true;
    }
}
