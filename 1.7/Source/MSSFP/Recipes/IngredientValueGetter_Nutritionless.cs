using RimWorld;
using Verse;

namespace MSSFP.Recipes;

public class IngredientValueGetter_Nutritionless : IngredientValueGetter
{
    public override float ValuePerUnitOf(ThingDef thingDef)
    {
        if (thingDef.IsNutritionGivingIngestible)
            return thingDef.GetStatValueAbstract(StatDefOf.Nutrition);
        return thingDef.IsStuff ? thingDef.VolumePerUnit : 1f;
    }

    public override string BillRequirementsDescription(RecipeDef recipe, IngredientCount ingredient)
    {
        string baseCount = ingredient.GetBaseCount().ToString();
        return ingredient.IsFixedIngredient ? $"{baseCount}x {ingredient.filter.Summary}" : $"{baseCount}x {"BillNutrition".Translate()} ({ingredient.filter.Summary})";
    }
}
