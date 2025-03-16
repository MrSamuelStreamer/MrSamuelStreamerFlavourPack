using Verse;

namespace MSSFP.Verbs;

public class Verb_AbilityShootThingHolder : Verb_AbilityShoot
{
    public Thing SelectedThing;

    public override bool Available()
    {
        if (!base.Available())
            return false;

        if (Caster is not Pawn pawn)
            return false;
        Thing selectedThing = pawn.inventory.innerContainer.RandomElement();
        return selectedThing != null;
    }

    protected override bool TryCastShot()
    {
        // try to get item from inventory
        if (Caster is not Pawn pawn)
            return false;

        Thing selectedThing = pawn.inventory.innerContainer.RandomElement();

        if (selectedThing == null)
            return false;

        SelectedThing = selectedThing.SplitOff(1);

        return base.TryCastShot();
    }

    public void ModifyProjectile(Projectile projectile)
    {
        if (projectile is not ThingHoldingProjectile proj)
            return;
        proj.HeldThing = SelectedThing;
    }
}
