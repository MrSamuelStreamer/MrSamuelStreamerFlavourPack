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
        Thing selectedThing = pawn.inventory.innerContainer.RandomElementWithFallback();
        return selectedThing != null;
    }

    protected override bool TryCastShot()
    {
        // try to get item from inventory
        if (Caster is not Pawn pawn)
            return false;

        Thing selectedThing = pawn.inventory.innerContainer.RandomElementWithFallback();

        if (selectedThing == null)
            return false;

        SelectedThing = selectedThing.SplitOff(1);

        bool succeeded = base.TryCastShot();
        if (!succeeded)
        {
            // Launch never happened (LOS lost, verb interrupted) — return the split item
            // to the pawn's inventory instead of letting it vanish unreferenced.
            if (SelectedThing != null && !SelectedThing.Destroyed)
            {
                pawn.inventory.innerContainer.TryAddOrTransfer(SelectedThing);
            }
            SelectedThing = null;
        }

        return succeeded;
    }

    public void ModifyProjectile(Projectile projectile)
    {
        if (projectile is not ThingHoldingProjectile proj)
            return;
        proj.HeldThing = SelectedThing;
    }
}
